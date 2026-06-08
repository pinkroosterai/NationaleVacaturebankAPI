# NationaleVacaturebank.Client

A .NET 10 SDK for the [Nationale Vacaturebank](https://www.nationalevacaturebank.nl) REST API — the Dutch national job board. Provides a strongly-typed, fluent client with full `Microsoft.Extensions` DI support and `IAsyncEnumerable` auto-pagination.

[![NuGet](https://img.shields.io/nuget/v/NationaleVacaturebank.Client)](https://www.nuget.org/packages/NationaleVacaturebank.Client)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Installation

```sh
dotnet add package NationaleVacaturebank.Client
```

## Getting Started

### With Microsoft.Extensions DI (ASP.NET Core, Worker Services)

Register the client in your `Program.cs` or `Startup.cs`:

```csharp
services.AddNationaleVacaturebank();
```

Then inject `IVacaturebankClient` wherever you need it:

```csharp
public class JobService(IVacaturebankClient client)
{
    public async Task<JobsPage> GetJobsAsync() =>
        await client.Jobs().WithTitle("software engineer").GetPageAsync();
}
```

### Standalone (no DI)

```csharp
using var httpClient = new HttpClient();
IVacaturebankClient client = new VacaturebankClient(httpClient, new VacaturebankOptions());
```

## Usage

### Search jobs — single page

```csharp
JobsPage page = await client.Jobs()
    .WithTitle("software engineer")
    .InCity("Amsterdam")
    .SortBy(JobSort.Date)
    .GetPageAsync(page: 1, limit: 20);

Console.WriteLine($"{page.Total} jobs found, showing page {page.Page} of {page.Pages}");

foreach (Job job in page.Jobs)
    Console.WriteLine($"{job.Title} @ {job.Company?.Name}");
```

### Search jobs — stream all results

`GetAllAsync` fetches pages lazily and yields jobs one by one. It stops when the last page is reached.

```csharp
await foreach (Job job in client.Jobs().WithTitle("developer").GetAllAsync())
{
    Console.WriteLine($"{job.Title} — {job.Company?.Name}");
}
```

### Search by location

Use `AtCoordinates` + `WithinKm` to filter by proximity. `WithinKm` requires coordinates to be set.

```csharp
GeoLocation amsterdam = await client.GetGeoLocationAsync("Amsterdam");

JobsPage nearby = await client.Jobs()
    .WithTitle("backend developer")
    .AtCoordinates(amsterdam.Latitude, amsterdam.Longitude)
    .WithinKm(30)
    .GetPageAsync();
```

### Autocomplete helpers

```csharp
// Suggest job function titles
IReadOnlyList<string> titles = await client.SearchFunctionTitlesAsync("soft");
// e.g. ["Software Engineer", "Software Architect", ...]

// Suggest city names
IReadOnlyList<string> cities = await client.SearchCitiesAsync("Amst");
// e.g. ["Amsterdam", "Amstelveen", ...]

// Resolve city name to coordinates
GeoLocation loc = await client.GetGeoLocationAsync("Rotterdam");
Console.WriteLine($"{loc.CityName}: {loc.Latitude}, {loc.Longitude}");
```

## Builder Reference

| Method | Description |
|---|---|
| `.WithTitle(string)` | Filter by job title |
| `.InCity(string)` | Filter by city name |
| `.AtCoordinates(double lat, double lon)` | Set search origin for distance filtering |
| `.WithinKm(double km)` | Filter by distance (requires `AtCoordinates`) |
| `.SortBy(JobSort)` | Sort by `Relevance`, `Date`, `Distance`, or `Random` |
| `.GetPageAsync(int page, int limit, CancellationToken)` | Fetch a single page (limit: 1–100) |
| `.GetAllAsync(int limit, CancellationToken)` | Stream all results as `IAsyncEnumerable<Job>` |

## Configuration

```csharp
services.AddNationaleVacaturebank(options =>
{
    options.BaseUrl = "https://api.nationalevacaturebank.nl"; // default
    options.Timeout = TimeSpan.FromSeconds(10);               // default: 30s
});
```

## Error Handling

| Exception | When thrown |
|---|---|
| `VacaturebankApiException` | The API returned a non-2xx HTTP status. Exposes `StatusCode` and `RequestUrl`. |
| `VacaturebankValidationException` | Invalid builder state before the request is sent (e.g. `WithinKm` without `AtCoordinates`, `limit` out of range). Exposes `ParameterName`. |

```csharp
try
{
    var page = await client.Jobs().WithTitle("developer").GetPageAsync();
}
catch (VacaturebankApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode} on {ex.RequestUrl}: {ex.Message}");
}
catch (VacaturebankValidationException ex)
{
    Console.WriteLine($"Invalid parameter '{ex.ParameterName}': {ex.Message}");
}
```

## License

MIT — see [LICENSE](LICENSE).
