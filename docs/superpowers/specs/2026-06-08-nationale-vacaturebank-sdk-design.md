# Nationale Vacaturebank C# SDK — Design Spec

**Date:** 2026-06-08  
**Status:** Approved

---

## Overview

A public NuGet package (`NationaleVacaturebank.Client`) providing a strongly-typed, fluent C# SDK for the Nationale Vacaturebank REST API. Targets .NET 10 (LTS). Intended for any .NET developer building job-search or recruitment integrations against the Dutch national job board.

---

## API Surface

**Base URL:** `https://api.nationalevacaturebank.nl`  
**Authentication:** None — the API is public.

### Endpoints wrapped

| Endpoint | Method | Purpose |
|---|---|---|
| `/api/jobs/v3/sites/nationalevacaturebank.nl/jobs` | GET | Search/filter job listings |
| `/api/jobs/v3/sites/nationalevacaturebank.nl/function-titles` | GET | Autocomplete job function titles |
| `/api/v1/cities/nl` | GET | City prefix search |
| `/api/v1/geolocations/nl/{cityName}` | GET | Resolve city name to lat/lon |

---

## Project Structure

Single solution, single library package, two test projects.

```
NationaleVacaturebankAPI.sln
├── src/
│   └── NationaleVacaturebank.Client/
│       ├── Client/
│       │   ├── IVacaturebankClient.cs
│       │   └── VacaturebankClient.cs
│       ├── Builders/
│       │   └── JobSearchBuilder.cs
│       ├── Models/
│       │   ├── Job.cs
│       │   ├── JobsPage.cs
│       │   ├── GeoLocation.cs
│       │   └── Enums/
│       │       ├── JobSort.cs
│       │       └── ContractType.cs
│       ├── Exceptions/
│       │   ├── VacaturebankApiException.cs
│       │   └── VacaturebankValidationException.cs
│       ├── Options/
│       │   └── VacaturebankOptions.cs
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
└── tests/
    ├── NationaleVacaturebank.Client.Tests.Unit/
    └── NationaleVacaturebank.Client.Tests.Integration/
```

---

## Core Client Interface

```csharp
public interface IVacaturebankClient
{
    JobSearchBuilder Jobs();
    Task<IReadOnlyList<string>> SearchFunctionTitlesAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SearchCitiesAsync(string startsWith, CancellationToken ct = default);
    Task<GeoLocation> GetGeoLocationAsync(string cityName, CancellationToken ct = default);
}
```

`VacaturebankClient` implements `IVacaturebankClient`. It holds an `HttpClient` (injected, not created internally) to be compatible with `IHttpClientFactory`.

---

## Fluent Query Builder

`JobSearchBuilder` is returned by `client.Jobs()` and is the primary way to search jobs.

```csharp
// Paged result
JobsPage page = await client.Jobs()
    .WithTitle("Software Engineer")
    .InCity("Amsterdam")
    .WithinKm(25)
    .SortBy(JobSort.Date)
    .GetPageAsync(page: 1, limit: 20);

// Auto-paginate all results (IAsyncEnumerable, lazy)
await foreach (Job job in client.Jobs().WithTitle("Developer").GetAllAsync())
{
    Console.WriteLine(job.Title);
}
```

### Builder methods

| Method | Maps to API filter |
|---|---|
| `WithTitle(string)` | `dcoTitle:{value}` |
| `InCity(string)` | `city:{value}` |
| `WithinKm(double)` | `distance:{value}` |
| `AtCoordinates(double lat, double lon)` | `latitude:{lat} longitude:{lon}` |
| `SortBy(JobSort)` | `sort` query param |
| `GetPageAsync(int page, int limit, CancellationToken)` | Executes request, returns `JobsPage` |
| `GetAllAsync(int limit, CancellationToken)` | Iterates all pages lazily as `IAsyncEnumerable<Job>` |

`GetAllAsync` fetches the first page, yields its jobs, then follows `_links.next` until exhausted. Default `limit` per page is 100 (maximum allowed).

`WithinKm` requires `AtCoordinates` to also be set — distance filtering is coordinate-based in the API. Using `WithinKm` without `AtCoordinates` throws `VacaturebankValidationException` at execution time. `InCity` and `AtCoordinates`/`WithinKm` are independent and can be combined or used alone.

---

## Data Models

### Job
```
Id, Title, DcoTitle, Description
Company: { Name, Website, Slug, Type }
Salary: { Min, Max }  (int, euros)
ContractType (string)
CareerLevel (string)
Categories (string[])
Industries (string[])
StartDate, EndDate (type TBD — make a real API call during implementation to determine the format, then use `DateOnly`, `DateTimeOffset`, or `string` accordingly)
Status (string)
WorkingHours: { Min, Max }
```

### JobsPage
```
Page, Limit, Pages, Total (int)
Jobs (IReadOnlyList<Job>)
HasNextPage (bool, derived)
```

### GeoLocation
```
CityName (string)
Latitude, Longitude (double)
```

---

## Error Handling

All error handling uses exceptions (standard C# style).

```csharp
// Thrown when the API returns a non-2xx HTTP status
public class VacaturebankApiException : Exception
{
    public int StatusCode { get; }
    public string RequestUrl { get; }
}

// Thrown before the HTTP request, for invalid builder state
public class VacaturebankValidationException : Exception
{
    public string ParameterName { get; }
}
```

`VacaturebankValidationException` is thrown by the builder when invariants are violated (e.g., `WithinKm` set without coordinates or city, `limit` outside 1–100). Validation happens at `GetPageAsync`/`GetAllAsync` call time, not at method-chain time.

---

## Configuration

```csharp
public class VacaturebankOptions
{
    public string BaseUrl { get; set; } = "https://api.nationalevacaturebank.nl";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### Standalone (no DI)
```csharp
var client = new VacaturebankClient(
    httpClient,          // caller provides HttpClient
    new VacaturebankOptions()
);
```

### ASP.NET Core / Microsoft.Extensions DI
```csharp
// Zero-config — uses all defaults
services.AddNationaleVacaturebank();

// With overrides
services.AddNationaleVacaturebank(options =>
{
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

`AddNationaleVacaturebank()` registers a named `HttpClient` via `IHttpClientFactory` and registers `IVacaturebankClient` as a scoped service. Timeout is applied to the named `HttpClient`.

---

## Testing Strategy

| Project | Framework | Scope |
|---|---|---|
| `Tests.Unit` | xUnit | Builder logic, exception mapping, `System.Text.Json` deserialization — no real HTTP |
| `Tests.Integration` | xUnit | Live calls to `api.nationalevacaturebank.nl` — skipped in CI without network |

Unit tests mock `HttpMessageHandler` directly (no extra mocking library). Integration tests serve as live conformance tests and as a canary for API schema changes.

---

## NuGet Package Metadata

- **Package ID:** `NationaleVacaturebank.Client`
- **Target framework:** `net10.0`
- **Dependencies:** `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `System.Text.Json` (all in-box with .NET 10)
- **License:** MIT
- **Tags:** `jobs`, `vacatures`, `nationale-vacaturebank`, `netherlands`, `api`, `sdk`
