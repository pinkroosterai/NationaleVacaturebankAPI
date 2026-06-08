using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Models;
using NationaleVacaturebank.Client.Models.Enums;

namespace NationaleVacaturebank.Client.Builders;

public sealed class JobSearchBuilder
{
    private readonly VacaturebankClient _client;
    private string? _jobTitle;
    private string? _city;
    private double _latitude;
    private double _longitude;
    private double _distance = 40;
    private bool _withinKmExplicitlySet;
    private JobSort _sort = JobSort.Relevance;

    internal JobSearchBuilder(VacaturebankClient client) => _client = client;

    public JobSearchBuilder WithTitle(string title)
    {
        _jobTitle = title;
        return this;
    }

    public JobSearchBuilder InCity(string city)
    {
        _city = city;
        return this;
    }

    public JobSearchBuilder AtCoordinates(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public JobSearchBuilder WithinKm(double km)
    {
        _distance = km;
        _withinKmExplicitlySet = true;
        return this;
    }

    public JobSearchBuilder SortBy(JobSort sort)
    {
        _sort = sort;
        return this;
    }

    public Task<JobsPage> GetPageAsync(int page = 1, int limit = 10, CancellationToken ct = default)
    {
        if (page < 1)
            throw new VacaturebankValidationException(nameof(page), "page must be >= 1.");
        if (limit < 1 || limit > 100)
            throw new VacaturebankValidationException(nameof(limit), "limit must be between 1 and 100.");
        if (_withinKmExplicitlySet && _latitude == 0 && _longitude == 0)
            throw new VacaturebankValidationException(
                "WithinKm", "WithinKm requires AtCoordinates to also be set.");

        return _client.FindJobsAsync(BuildParams(page, limit), ct);
    }

    public IAsyncEnumerable<Job> GetAllAsync(int limit = 100, CancellationToken ct = default)
    {
        if (limit < 1 || limit > 100)
            throw new VacaturebankValidationException(nameof(limit), "limit must be between 1 and 100.");
        if (_withinKmExplicitlySet && _latitude == 0 && _longitude == 0)
            throw new VacaturebankValidationException(
                "WithinKm", "WithinKm requires AtCoordinates to also be set.");

        return _client.FindAllJobsAsync(BuildParams(1, limit), ct);
    }

    private JobSearchParams BuildParams(int page, int limit) => new()
    {
        Page = page,
        Limit = limit,
        Sort = _sort,
        City = _city ?? string.Empty,
        JobTitle = _jobTitle ?? string.Empty,
        Latitude = _latitude,
        Longitude = _longitude,
        Distance = _distance,
    };
}
