# NationaleVacaturebank.Client

A .NET 10 SDK for the [Nationale Vacaturebank](https://www.nationalevacaturebank.nl) REST API.

## Installation

```sh
dotnet add package NationaleVacaturebank.Client
```

## Quick Start

```csharp
// With Microsoft.Extensions.DI
services.AddNationaleVacaturebank();

// Standalone
var client = new VacaturebankClient(httpClient, new VacaturebankOptions());
```

## Usage

### Search jobs (paged)

```csharp
JobsPage page = await client.Jobs()
    .WithTitle("software engineer")
    .InCity("Amsterdam")
    .WithinKm(25)
    .SortBy(JobSort.Date)
    .GetPageAsync(page: 1, limit: 20);
```

### Stream all results

```csharp
await foreach (Job job in client.Jobs().WithTitle("developer").GetAllAsync())
{
    Console.WriteLine(job.Title);
}
```

### Autocomplete

```csharp
IReadOnlyList<string> titles = await client.SearchFunctionTitlesAsync("soft");
IReadOnlyList<string> cities = await client.SearchCitiesAsync("Amst");
GeoLocation loc = await client.GetGeoLocationAsync("Amsterdam");
```

## Configuration

```csharp
services.AddNationaleVacaturebank(options =>
{
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

## Error Handling

```csharp
try
{
    var page = await client.Jobs().GetPageAsync();
}
catch (VacaturebankApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
catch (VacaturebankValidationException ex)
{
    Console.WriteLine($"Validation error ({ex.ParameterName}): {ex.Message}");
}
```

## License

MIT
