using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NationaleVacaturebank.Client.Builders;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Models;
using NationaleVacaturebank.Client.Models.Internal;
using NationaleVacaturebank.Client.Options;

namespace NationaleVacaturebank.Client.Client;

public sealed class VacaturebankClient : IVacaturebankClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly VacaturebankOptions _options;

    public VacaturebankClient(HttpClient httpClient, VacaturebankOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public JobSearchBuilder Jobs() => new(this);

    public async Task<IReadOnlyList<string>> SearchFunctionTitlesAsync(
        string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new VacaturebankValidationException(nameof(query), "query cannot be empty.");

        var url = $"{_options.BaseUrl}/api/jobs/v3/sites/nationalevacaturebank.nl/function-titles?query={Uri.EscapeDataString(query)}";
        var response = await GetAsync<FunctionTitlesApiModel>(url, ct);
        return (IReadOnlyList<string>?)response.Suggestions?.AsReadOnly() ?? Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> SearchCitiesAsync(
        string startsWith, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(startsWith))
            throw new VacaturebankValidationException(nameof(startsWith), "startsWith cannot be empty.");

        var url = $"{_options.BaseUrl}/api/v1/cities/nl?startsWith={Uri.EscapeDataString(startsWith)}";
        var cities = await GetAsync<List<string>>(url, ct);
        return cities.AsReadOnly();
    }

    public async Task<GeoLocation> GetGeoLocationAsync(
        string cityName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            throw new VacaturebankValidationException(nameof(cityName), "cityName cannot be empty.");

        var url = $"{_options.BaseUrl}/api/v1/geolocations/nl/{Uri.EscapeDataString(cityName)}";
        var response = await GetAsync<GeoLocationApiModel>(url, ct);

        if (!double.TryParse(response.CityCenter?.Latitude, CultureInfo.InvariantCulture, out var lat))
            throw new VacaturebankApiException(200, url, "Failed to parse latitude from API response.");
        if (!double.TryParse(response.CityCenter?.Longitude, CultureInfo.InvariantCulture, out var lon))
            throw new VacaturebankApiException(200, url, "Failed to parse longitude from API response.");

        return new GeoLocation(response.CityName ?? cityName, lat, lon);
    }

    internal async Task<JobsPage> FindJobsAsync(JobSearchParams searchParams, CancellationToken ct)
    {
        var url = BuildJobsUrl(searchParams);
        var response = await GetAsync<JobsApiResponse>(url, ct);
        return MapToJobsPage(response);
    }

    internal async IAsyncEnumerable<Job> FindAllJobsAsync(
        JobSearchParams searchParams,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var page = 1;
        int? totalPages = null;

        do
        {
            var p = searchParams with { Page = page };
            var url = BuildJobsUrl(p);
            var response = await GetAsync<JobsApiResponse>(url, ct);
            totalPages ??= response.Pages;

            foreach (var job in response.Embedded?.Jobs ?? [])
                yield return MapJob(job);

            page++;
        }
        while (page <= totalPages);
    }

    private string BuildJobsUrl(JobSearchParams p)
    {
        var sortValue = p.Sort switch
        {
            Models.Enums.JobSort.Date => "date",
            Models.Enums.JobSort.Distance => "distance",
            Models.Enums.JobSort.Random => "random",
            _ => "relevance",
        };

        var filters = new List<string>();
        if (p.Latitude != 0 && p.Longitude != 0)
        {
            filters.Add($"latitude:{p.Latitude.ToString("F6", CultureInfo.InvariantCulture)}");
            filters.Add($"longitude:{p.Longitude.ToString("F6", CultureInfo.InvariantCulture)}");
            filters.Add($"distance:{p.Distance.ToString("F0", CultureInfo.InvariantCulture)}");
        }
        if (!string.IsNullOrEmpty(p.City))
            filters.Add($"city:{p.City}");
        if (!string.IsNullOrEmpty(p.JobTitle))
            filters.Add($"dcoTitle:{p.JobTitle}");

        var query = $"page={p.Page}&limit={p.Limit}&sort={sortValue}";
        if (filters.Count > 0)
            query += $"&filters={Uri.EscapeDataString(string.Join(" ", filters))}";

        return $"{_options.BaseUrl}/api/jobs/v3/sites/nationalevacaturebank.nl/jobs?{query}";
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new VacaturebankApiException(
                (int)response.StatusCode, url,
                $"API request failed with status {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new VacaturebankApiException(
                (int)response.StatusCode, url, "API returned null or empty response.");
    }

    private static JobsPage MapToJobsPage(JobsApiResponse r) => new()
    {
        Page = r.Page,
        Limit = r.Limit,
        Pages = r.Pages,
        Total = r.Total,
        Jobs = (r.Embedded?.Jobs ?? []).Select(MapJob).ToList().AsReadOnly(),
    };

    private static Job MapJob(JobApiModel m) => new()
    {
        Id = m.Id ?? string.Empty,
        Title = m.Title ?? string.Empty,
        DcoTitle = m.DcoTitle,
        Description = m.Description,
        Company = m.Company is { } c ? new JobCompany(c.Name, c.Website, c.Slug, c.Type) : null,
        Salary = m.Salary is { } s ? new SalaryRange(s.Min, s.Max) : null,
        ContractType = m.ContractType,
        CareerLevel = m.CareerLevel,
        Categories = (IReadOnlyList<string>?)m.Categories?.AsReadOnly() ?? Array.Empty<string>(),
        Industries = (IReadOnlyList<string>?)m.Industries?.AsReadOnly() ?? Array.Empty<string>(),
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Status = m.Status,
        WorkingHours = m.WorkingHours is { } w ? new WorkingHoursRange(w.Min, w.Max) : null,
    };
}
