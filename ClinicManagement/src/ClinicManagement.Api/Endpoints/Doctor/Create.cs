﻿using System.Threading;
using System.Threading.Tasks;
using Ardalis.ApiEndpoints;
using AutoMapper;
using BlazorShared.Models.Doctor;
using ClinicManagement.Core.Aggregates;
using ClinicManagement.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using PluralsightDdd.SharedKernel.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ClinicManagement.Api.DoctorEndpoints
{
  public class Create : BaseAsyncEndpoint
    .WithRequest<CreateDoctorRequest>
    .WithResponse<CreateDoctorResponse>
  {
    private readonly IRepository<Doctor> _repository;
    private readonly IMapper _mapper;
    private readonly IMessagePublisher _messagePublisher;

    public Create(IRepository<Doctor> repository,
      IMapper mapper,
      IMessagePublisher messagePublisher)
    {
      _repository = repository;
      _mapper = mapper;
      _messagePublisher = messagePublisher;
    }

    [HttpPost("api/doctors")]
    [SwaggerOperation(
        Summary = "Creates a new Doctor",
        Description = "Creates a new Doctor",
        OperationId = "doctors.create",
        Tags = new[] { "DoctorEndpoints" })
    ]
    public override async Task<ActionResult<CreateDoctorResponse>> HandleAsync(CreateDoctorRequest request, CancellationToken cancellationToken)
    {
      var response = new CreateDoctorResponse(request.CorrelationId());

      var toAdd = _mapper.Map<Doctor>(request);
      toAdd = await _repository.AddAsync(toAdd);

      var dto = _mapper.Map<DoctorDto>(toAdd);
      response.Doctor = dto;

      var appEvent = new EntityCreatedEvent(_mapper.Map<NamedEntity>(toAdd));
      _messagePublisher.Publish(appEvent);

      return Ok(response);
    }
  }

  public class EntityCreatedEvent : IApplicationEvent
  {
    public string EventType => "Doctor-Created";
    public NamedEntity Entity { get; set; }

    public EntityCreatedEvent(NamedEntity entity)
    {
      Entity = entity;
    }
  }

  public class NamedEntity
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }
}
