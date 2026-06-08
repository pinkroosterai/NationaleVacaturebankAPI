using Microsoft.Extensions.DependencyInjection;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Extensions;
using NationaleVacaturebank.Client.Models;

namespace NationaleVacaturebank.Client.Tests.Integration;

public class VacaturebankClientIntegrationTests
{
    private readonly IVacaturebankClient _client;

    public VacaturebankClientIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddNationaleVacaturebank();
        _client = services.BuildServiceProvider()
            .GetRequiredService<IVacaturebankClient>();
    }

    [Fact]
    public async Task GetJobsPage_ReturnsResults()
    {
        var page = await _client.Jobs()
            .WithTitle("software")
            .GetPageAsync(page: 1, limit: 5);

        Assert.True(page.Total > 0);
        Assert.NotEmpty(page.Jobs);
    }

    [Fact]
    public async Task GetAllJobs_StreamsMultiplePages()
    {
        var jobs = new List<Job>();
        await foreach (var job in _client.Jobs().WithTitle("developer").GetAllAsync(limit: 10))
        {
            jobs.Add(job);
            if (jobs.Count >= 15) break;
        }

        Assert.True(jobs.Count >= 10, $"Expected at least 10 jobs, got {jobs.Count}");
    }

    [Fact]
    public async Task SearchFunctionTitles_ReturnsSuggestions()
    {
        var titles = await _client.SearchFunctionTitlesAsync("software");

        Assert.NotEmpty(titles);
    }

    [Fact]
    public async Task SearchCities_ReturnsResults()
    {
        var cities = await _client.SearchCitiesAsync("Amst");

        Assert.NotEmpty(cities);
        Assert.Contains(cities, c => c.Contains("Amsterdam", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetGeoLocation_ReturnsCoordinates()
    {
        var location = await _client.GetGeoLocationAsync("Amsterdam");

        Assert.Equal("Amsterdam", location.CityName, ignoreCase: true);
        Assert.True(location.Latitude > 0);
        Assert.True(location.Longitude > 0);
    }
}
