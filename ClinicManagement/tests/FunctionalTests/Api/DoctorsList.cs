﻿using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.HttpClientTestExtensions;
using BlazorShared.Models.Doctor;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests.Api
{
  [Collection("Sequential")]
  public class DoctorsList : IClassFixture<CustomWebApplicationFactory<Program>>
  {
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;

    public DoctorsList(CustomWebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
    {
      _client = factory.CreateClient();
      _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Returns3Doctors()
    {
      var result = await _client.GetAndDeserializeAsync<ListDoctorResponse>("/api/doctors", _outputHelper);

      Assert.Equal(3, result.Doctors.Count());
      Assert.Contains(result.Doctors, x => x.Name == "Dr. Smith");
    }
  }
}
