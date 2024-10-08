﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlazorShared.Models.Appointment;
using FastEndpoints;
using FrontDesk.Core.Exceptions;
using FrontDesk.Core.Interfaces;
using FrontDesk.Core.ScheduleAggregate;
using FrontDesk.Core.ScheduleAggregate.Specifications;
using FrontDesk.Core.SyncedAggregates;
using FrontDesk.Core.SyncedAggregates.Specifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluralsightDdd.SharedKernel.Interfaces;
using IMapper = AutoMapper.IMapper;

namespace FrontDesk.Api.AppointmentEndpoints
{
  public class List : Endpoint<ListAppointmentRequest, Results<Ok<ListAppointmentResponse>, NotFound>>
  {
    private readonly IReadRepository<Schedule> _scheduleRepository;
    private readonly IReadRepository<Client> _clientRepository;
    private readonly IMapper _mapper;
    private readonly IApplicationSettings _settings;
    private readonly ILogger<List> _logger;

    public List(IReadRepository<Schedule> scheduleRepository,
      IReadRepository<Client> clientRepository,
      IMapper mapper,
      IApplicationSettings settings,
      ILogger<List> logger)
    {
      _scheduleRepository = scheduleRepository;
      _clientRepository = clientRepository;
      _mapper = mapper;
      _settings = settings;
      _logger = logger;
    }

    public override void Configure()
    {
      Get(ListAppointmentRequest.Route);
      AllowAnonymous();
      Description(d =>
        d.WithSummary("List Appointments")
         .WithDescription("List Appointments")
         .WithName("appointments.List")
         .WithTags("AppointmentEndpoints"));
    }

    public override async Task<Results<Ok<ListAppointmentResponse>, NotFound>> ExecuteAsync([FromRoute] ListAppointmentRequest request,
      CancellationToken cancellationToken)
    {
      var response = new ListAppointmentResponse(request.CorrelationId());
      Schedule schedule = null;
      if (request.ScheduleId == Guid.Empty)
      {
        return TypedResults.NotFound();
      }

      // TODO: Get date from API request and use a specification that only includes appointments on that date.
      var spec = new ScheduleByIdWithAppointmentsSpec(request.ScheduleId);
      schedule = await _scheduleRepository.GetBySpecAsync(spec);
      if (schedule == null) throw new ScheduleNotFoundException($"No schedule found for id {request.ScheduleId}.");

      int conflictedAppointmentsCount = schedule.Appointments
        .Count(a => a.IsPotentiallyConflicting);
      _logger.LogInformation($"API:ListAppointments There are now {conflictedAppointmentsCount} conflicted appointments.");

      var myAppointments = _mapper.Map<List<AppointmentDto>>(schedule.Appointments);

      // load names - only do this kind of thing if you have caching!
      // N+1 query problem
      // Possibly use custom SQL or view or stored procedure instead
      foreach (var appt in myAppointments)
      {
        var clientSpec = new ClientByIdIncludePatientsSpecification(appt.ClientId);
        var client = await _clientRepository.GetBySpecAsync(clientSpec);
        var patient = client.Patients.First(p => p.Id == appt.PatientId);

        appt.ClientName = client.FullName;
        appt.PatientName = patient.Name;
      }

      response.Appointments = myAppointments.OrderBy(a => a.Start).ToList();
      response.Count = response.Appointments.Count;

      return TypedResults.Ok(response);
    }

    //  public override async Task<ActionResult<ListAppointmentResponse>> HandleAsync([FromQuery] ListAppointmentRequest request,
    //CancellationToken cancellationToken)
    //  {
    //    var response = new ListAppointmentResponse(request.CorrelationId());
    //    Schedule schedule = null;
    //    if (request.ScheduleId == Guid.Empty)
    //    {
    //      var spec = new ScheduleForClinicAndDateWithAppointmentsSpec(_settings.ClinicId, _settings.TestDate);
    //      schedule = await _scheduleRepository.GetBySpecAsync(spec);
    //      if (schedule == null) throw new ScheduleNotFoundException($"No schedule found for clinic {_settings.ClinicId}.");
    //    }
    //    else
    //    {
    //      var spec = new ScheduleByIdWithAppointmentsSpec(request.ScheduleId);
    //      schedule = await _scheduleRepository.GetBySpecAsync(spec);
    //      if (schedule == null) throw new ScheduleNotFoundException($"No schedule found for id {request.ScheduleId}.");
    //    }
    //    schedule.Handle(null); // mark conflicts
    //    var myAppointments = _mapper.Map<List<AppointmentDto>>(schedule.Appointments);

    //    // load names - only do this kind of thing if you have caching!
    //    // N+1 query problem
    //    // Possibly use custom SQL or view or stored procedure instead
    //    foreach (var appt in myAppointments)
    //    {
    //      var clientSpec = new ClientByIdIncludePatientsSpecification(appt.ClientId);
    //      var client = await _clientRepository.GetBySpecAsync(clientSpec);
    //      var patient = client.Patients.First(p => p.Id == appt.PatientId);

    //      appt.ClientName = client.FullName;
    //      appt.PatientName = patient.Name;
    //    }

    //    response.Appointments = myAppointments.OrderBy(a => a.Start).ToList();
    //    response.Count = response.Appointments.Count;

    //    return Ok(response);
    //  }

  }
}
