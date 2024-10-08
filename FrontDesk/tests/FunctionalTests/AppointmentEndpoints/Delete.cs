﻿using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.HttpClientTestExtensions;
using BlazorShared.Models.Appointment;
using BlazorShared.Models.Schedule;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests.AppointmentEndpoints
{
  [Collection("Sequential")]
  public class Delete : IClassFixture<CustomWebApplicationFactory<Program>>
  {
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;

    public Delete(CustomWebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
    {
      _client = factory.CreateClient();
      _outputHelper = outputHelper;
    }

    [Fact]
    public async Task DeletesExistingAppointment()
    {
      // get schedule
      var listResult = await _client.GetAndDeserializeAsync<ListScheduleResponse>(ListScheduleRequest.Route, _outputHelper);
      var schedule = listResult.Schedules.First();
      string scheduleId = schedule.Id.ToString();

      string getRoute = ListAppointmentRequest.Route.Replace("{ScheduleId}", scheduleId);

      // get existing appointment
      var result = await _client.GetAndDeserializeAsync<ListAppointmentResponse>(getRoute, _outputHelper);

      var firstAppt = result.Appointments.First();
      _outputHelper.WriteLine(firstAppt.ToString());

      // delete it
      string route = DeleteAppointmentRequest.Route.Replace("{AppointmentId}", firstAppt.AppointmentId.ToString());
      route = route.Replace("{ScheduleId}", scheduleId);
      var deleteResponse = await _client.DeleteAsync(route);
      deleteResponse.EnsureSuccessStatusCode();

      var response = await _client.GetAndDeserializeAsync<GetByIdScheduleResponse>(GetByIdScheduleRequest.Route.Replace("{scheduleId}", scheduleId), _outputHelper);

      Assert.Empty(response.Schedule.AppointmentIds);
    }
  }
}
