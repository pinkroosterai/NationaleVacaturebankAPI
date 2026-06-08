# Nationale Vacaturebank C# SDK — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and publish `NationaleVacaturebank.Client`, a public NuGet package wrapping the Nationale Vacaturebank REST API with a strongly-typed, fluent C# SDK.

**Architecture:** Single class library targeting `net10.0`. `VacaturebankClient` handles HTTP via an injected `HttpClient`. `JobSearchBuilder` provides a fluent API returned from `client.Jobs()`. Internal deserialization models are decoupled from public models. Error handling is exception-based throughout.

**Tech Stack:** .NET 10, C# 13, `System.Text.Json`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`, xUnit 2.x.

---

## File Map

```
NationaleVacaturebankAPI.sln
src/NationaleVacaturebank.Client/
  NationaleVacaturebank.Client.csproj
  Client/
    IVacaturebankClient.cs
    VacaturebankClient.cs
  Builders/
    JobSearchBuilder.cs
    JobSearchParams.cs               (internal)
  Models/
    Job.cs                           (Job, JobCompany, SalaryRange, WorkingHoursRange)
    JobsPage.cs
    GeoLocation.cs
    Enums/
      JobSort.cs
    Internal/
      JobsApiResponse.cs             (all HAL response types)
      GeoLocationApiModel.cs
      FunctionTitlesApiModel.cs
  Exceptions/
    VacaturebankApiException.cs
    VacaturebankValidationException.cs
  Options/
    VacaturebankOptions.cs
  Extensions/
    ServiceCollectionExtensions.cs

tests/NationaleVacaturebank.Client.Tests.Unit/
  NationaleVacaturebank.Client.Tests.Unit.csproj
  Helpers/
    MockHttpMessageHandler.cs
  ExceptionTests.cs
  Client/
    VacaturebankClientTests.cs
  Builders/
    JobSearchBuilderTests.cs
  Extensions/
    ServiceCollectionExtensionsTests.cs

tests/NationaleVacaturebank.Client.Tests.Integration/
  NationaleVacaturebank.Client.Tests.Integration.csproj
  VacaturebankClientIntegrationTests.cs

README.md
.gitignore
```

---

### Task 1: Solution scaffolding

**Files:**
- Create: `NationaleVacaturebankAPI.sln`
- Create: `src/NationaleVacaturebank.Client/NationaleVacaturebank.Client.csproj`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/NationaleVacaturebank.Client.Tests.Unit.csproj`
- Create: `tests/NationaleVacaturebank.Client.Tests.Integration/NationaleVacaturebank.Client.Tests.Integration.csproj`
- Create: `.gitignore`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/Helpers/MockHttpMessageHandler.cs`

- [ ] **Step 1: Scaffold solution and projects**

```bash
dotnet new sln -n NationaleVacaturebankAPI
dotnet new classlib -n NationaleVacaturebank.Client -f net10.0 -o src/NationaleVacaturebank.Client
dotnet new xunit -n NationaleVacaturebank.Client.Tests.Unit -f net10.0 -o tests/NationaleVacaturebank.Client.Tests.Unit
dotnet new xunit -n NationaleVacaturebank.Client.Tests.Integration -f net10.0 -o tests/NationaleVacaturebank.Client.Tests.Integration
dotnet sln add src/NationaleVacaturebank.Client/NationaleVacaturebank.Client.csproj
dotnet sln add tests/NationaleVacaturebank.Client.Tests.Unit/NationaleVacaturebank.Client.Tests.Unit.csproj
dotnet sln add tests/NationaleVacaturebank.Client.Tests.Integration/NationaleVacaturebank.Client.Tests.Integration.csproj
dotnet add tests/NationaleVacaturebank.Client.Tests.Unit reference src/NationaleVacaturebank.Client
dotnet add tests/NationaleVacaturebank.Client.Tests.Integration reference src/NationaleVacaturebank.Client
```

Expected: all commands succeed with no errors.

- [ ] **Step 2: Add NuGet packages to the library**

```bash
dotnet add src/NationaleVacaturebank.Client package Microsoft.Extensions.Http
dotnet add src/NationaleVacaturebank.Client package Microsoft.Extensions.DependencyInjection.Abstractions
```

Expected: both packages appear in `NationaleVacaturebank.Client.csproj`.

- [ ] **Step 3: Replace the library csproj with NuGet-ready metadata**

Replace the entire contents of `src/NationaleVacaturebank.Client/NationaleVacaturebank.Client.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackageId>NationaleVacaturebank.Client</PackageId>
    <Version>1.0.0</Version>
    <Authors>Jan Versteeg</Authors>
    <Description>Strongly-typed C# SDK for the Nationale Vacaturebank (Dutch national job board) API. Includes a fluent query builder and full Microsoft.Extensions DI support.</Description>
    <PackageTags>jobs;vacatures;nationale-vacaturebank;netherlands;api;sdk</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.*" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Delete generated placeholder files**

```bash
rm src/NationaleVacaturebank.Client/Class1.cs
rm tests/NationaleVacaturebank.Client.Tests.Unit/UnitTest1.cs
rm tests/NationaleVacaturebank.Client.Tests.Integration/UnitTest1.cs
```

- [ ] **Step 5: Create `.gitignore`**

Create `.gitignore` in the repo root:

```
bin/
obj/
nupkg/
*.nupkg
.vs/
.idea/
*.user
```

- [ ] **Step 6: Create `MockHttpMessageHandler`**

Create `tests/NationaleVacaturebank.Client.Tests.Unit/Helpers/MockHttpMessageHandler.cs`:

```csharp
using System.Net;
using System.Text;

namespace NationaleVacaturebank.Client.Tests.Unit.Helpers;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public MockHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        : this(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })
    { }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => _respond = respond;

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_respond(request));
    }
}
```

- [ ] **Step 7: Verify the solution builds**

```bash
dotnet build
```

Expected: `Build succeeded.` with no errors.

- [ ] **Step 8: Commit**

```bash
git add .
git commit -m "chore: scaffold solution, projects, and test helpers"
```

---

### Task 2: Probe the live API to determine date field type

**Files:** None created permanently — this investigation informs the type used in Tasks 4 and 5.

- [ ] **Step 1: Fetch one live job**

Run in PowerShell:

```powershell
(Invoke-RestMethod "https://api.nationalevacaturebank.nl/api/jobs/v3/sites/nationalevacaturebank.nl/jobs?page=1&limit=1") | ConvertTo-Json -Depth 10
```

- [ ] **Step 2: Inspect `startDate` and `endDate`**

Find `_embedded.jobs[0].startDate` and `_embedded.jobs[0].endDate` in the output and determine the C# type:

| Format observed | C# type to use in `Job.cs` and `JobsApiResponse.cs` |
|---|---|
| `"2025-06-01"` (date only) | `DateOnly?` |
| `"2025-06-01T00:00:00Z"` (ISO datetime) | `DateTimeOffset?` |
| `null` or absent | `string?` |
| Other | `string?` |

- [ ] **Step 3: Update the spec with the resolved type**

Open `docs/superpowers/specs/2026-06-08-nationale-vacaturebank-sdk-design.md` and replace the TBD date field line with the type chosen above.

```bash
git add docs/superpowers/specs/2026-06-08-nationale-vacaturebank-sdk-design.md
git commit -m "docs: resolve date field type from live API probe"
```

---

### Task 3: Exceptions and Options

**Files:**
- Create: `src/NationaleVacaturebank.Client/Exceptions/VacaturebankApiException.cs`
- Create: `src/NationaleVacaturebank.Client/Exceptions/VacaturebankValidationException.cs`
- Create: `src/NationaleVacaturebank.Client/Options/VacaturebankOptions.cs`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/ExceptionTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/NationaleVacaturebank.Client.Tests.Unit/ExceptionTests.cs`:

```csharp
using NationaleVacaturebank.Client.Exceptions;

namespace NationaleVacaturebank.Client.Tests.Unit;

public class ExceptionTests
{
    [Fact]
    public void VacaturebankApiException_StoresStatusCodeAndUrl()
    {
        var ex = new VacaturebankApiException(404, "https://example.com/api");

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("https://example.com/api", ex.RequestUrl);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void VacaturebankApiException_UsesCustomMessage_WhenProvided()
    {
        var ex = new VacaturebankApiException(500, "https://example.com/api", "Custom error");

        Assert.Equal("Custom error", ex.Message);
    }

    [Fact]
    public void VacaturebankValidationException_StoresParameterName()
    {
        var ex = new VacaturebankValidationException("query", "query cannot be empty.");

        Assert.Equal("query", ex.ParameterName);
        Assert.Equal("query cannot be empty.", ex.Message);
    }
}
```

- [ ] **Step 2: Run to confirm build failure**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "ExceptionTests" -v minimal
```

Expected: build error — `VacaturebankApiException` not found.

- [ ] **Step 3: Create `VacaturebankApiException`**

Create `src/NationaleVacaturebank.Client/Exceptions/VacaturebankApiException.cs`:

```csharp
namespace NationaleVacaturebank.Client.Exceptions;

public sealed class VacaturebankApiException : Exception
{
    public int StatusCode { get; }
    public string RequestUrl { get; }

    public VacaturebankApiException(int statusCode, string requestUrl, string? message = null)
        : base(message ?? $"API request to '{requestUrl}' failed with status {statusCode}.")
    {
        StatusCode = statusCode;
        RequestUrl = requestUrl;
    }
}
```

- [ ] **Step 4: Create `VacaturebankValidationException`**

Create `src/NationaleVacaturebank.Client/Exceptions/VacaturebankValidationException.cs`:

```csharp
namespace NationaleVacaturebank.Client.Exceptions;

public sealed class VacaturebankValidationException : Exception
{
    public string ParameterName { get; }

    public VacaturebankValidationException(string parameterName, string message)
        : base(message)
    {
        ParameterName = parameterName;
    }
}
```

- [ ] **Step 5: Create `VacaturebankOptions`**

Create `src/NationaleVacaturebank.Client/Options/VacaturebankOptions.cs`:

```csharp
namespace NationaleVacaturebank.Client.Options;

public sealed class VacaturebankOptions
{
    public string BaseUrl { get; set; } = "https://api.nationalevacaturebank.nl";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

- [ ] **Step 6: Run tests to confirm they pass**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "ExceptionTests" -v minimal
```

Expected: `Failed: 0, Passed: 3`.

- [ ] **Step 7: Commit**

```bash
git add src/NationaleVacaturebank.Client/Exceptions/ src/NationaleVacaturebank.Client/Options/ tests/NationaleVacaturebank.Client.Tests.Unit/ExceptionTests.cs
git commit -m "feat: add exceptions and VacaturebankOptions"
```

---

### Task 4: Public models and enums

> Use the date type determined in Task 2 for `StartDate`/`EndDate` on `Job`. This task uses `string?` as a placeholder — replace it if Task 2 revealed a more specific type.

**Files:**
- Create: `src/NationaleVacaturebank.Client/Models/Enums/JobSort.cs`
- Create: `src/NationaleVacaturebank.Client/Models/GeoLocation.cs`
- Create: `src/NationaleVacaturebank.Client/Models/Job.cs`
- Create: `src/NationaleVacaturebank.Client/Models/JobsPage.cs`

- [ ] **Step 1: Create `JobSort` enum**

Create `src/NationaleVacaturebank.Client/Models/Enums/JobSort.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models.Enums;

public enum JobSort
{
    Relevance,
    Date,
    Distance,
    Random,
}
```

- [ ] **Step 2: Create `GeoLocation`**

Create `src/NationaleVacaturebank.Client/Models/GeoLocation.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models;

public sealed record GeoLocation(string CityName, double Latitude, double Longitude);
```

- [ ] **Step 3: Create `Job` and value types**

Create `src/NationaleVacaturebank.Client/Models/Job.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models;

public sealed record Job
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? DcoTitle { get; init; }
    public string? Description { get; init; }
    public JobCompany? Company { get; init; }
    public SalaryRange? Salary { get; init; }
    public string? ContractType { get; init; }
    public string? CareerLevel { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> Industries { get; init; } = [];
    public string? StartDate { get; init; }  // Replace with DateOnly? or DateTimeOffset? per Task 2
    public string? EndDate { get; init; }    // Replace with DateOnly? or DateTimeOffset? per Task 2
    public string? Status { get; init; }
    public WorkingHoursRange? WorkingHours { get; init; }
}

public sealed record JobCompany(string? Name, string? Website, string? Slug, string? Type);

public sealed record SalaryRange(int Min, int Max);

public sealed record WorkingHoursRange(int Min, int Max);
```

- [ ] **Step 4: Create `JobsPage`**

Create `src/NationaleVacaturebank.Client/Models/JobsPage.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models;

public sealed record JobsPage
{
    public required int Page { get; init; }
    public required int Limit { get; init; }
    public required int Pages { get; init; }
    public required int Total { get; init; }
    public required IReadOnlyList<Job> Jobs { get; init; }
    public bool HasNextPage => Page < Pages;
}
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/NationaleVacaturebank.Client
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add src/NationaleVacaturebank.Client/Models/
git commit -m "feat: add public models and enums"
```

---

### Task 5: Internal API deserialization models and `JobSearchParams`

**Files:**
- Create: `src/NationaleVacaturebank.Client/Models/Internal/JobsApiResponse.cs`
- Create: `src/NationaleVacaturebank.Client/Models/Internal/GeoLocationApiModel.cs`
- Create: `src/NationaleVacaturebank.Client/Models/Internal/FunctionTitlesApiModel.cs`
- Create: `src/NationaleVacaturebank.Client/Builders/JobSearchParams.cs`

These are `internal sealed` types — they mirror the exact JSON the API returns, including HAL `_embedded`/`_links` fields. They are never exposed publicly.

- [ ] **Step 1: Create `JobsApiResponse` and all nested types**

Create `src/NationaleVacaturebank.Client/Models/Internal/JobsApiResponse.cs`:

```csharp
using System.Text.Json.Serialization;

namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class JobsApiResponse
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Pages { get; init; }
    public int Total { get; init; }

    [JsonPropertyName("_links")]
    public LinksApiModel? Links { get; init; }

    [JsonPropertyName("_embedded")]
    public EmbeddedApiModel? Embedded { get; init; }
}

internal sealed class LinksApiModel
{
    public LinkApiModel? Next { get; init; }
}

internal sealed class LinkApiModel
{
    public string? Href { get; init; }
}

internal sealed class EmbeddedApiModel
{
    public List<JobApiModel>? Jobs { get; init; }
}

internal sealed class JobApiModel
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? DcoTitle { get; init; }
    public string? Description { get; init; }
    public CompanyApiModel? Company { get; init; }
    public SalaryApiModel? Salary { get; init; }
    public string? ContractType { get; init; }
    public string? CareerLevel { get; init; }
    public List<string>? Categories { get; init; }
    public List<string>? Industries { get; init; }
    public string? StartDate { get; init; }  // Update type per Task 2 alongside Job.cs
    public string? EndDate { get; init; }    // Update type per Task 2 alongside Job.cs
    public string? Status { get; init; }
    public WorkingHoursApiModel? WorkingHours { get; init; }
}

internal sealed class CompanyApiModel
{
    public string? Name { get; init; }
    public string? Website { get; init; }
    public string? Slug { get; init; }
    public string? Type { get; init; }
}

internal sealed class SalaryApiModel
{
    public int Min { get; init; }
    public int Max { get; init; }
}

internal sealed class WorkingHoursApiModel
{
    public int Min { get; init; }
    public int Max { get; init; }
}
```

- [ ] **Step 2: Create `GeoLocationApiModel`**

Create `src/NationaleVacaturebank.Client/Models/Internal/GeoLocationApiModel.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class GeoLocationApiModel
{
    public CityCoordinatesApiModel? CityCenter { get; init; }
    public string? CityName { get; init; }
}

internal sealed class CityCoordinatesApiModel
{
    public string? Latitude { get; init; }   // API returns coordinates as strings, not numbers
    public string? Longitude { get; init; }
}
```

- [ ] **Step 3: Create `FunctionTitlesApiModel`**

Create `src/NationaleVacaturebank.Client/Models/Internal/FunctionTitlesApiModel.cs`:

```csharp
namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class FunctionTitlesApiModel
{
    public List<string>? Suggestions { get; init; }
}
```

- [ ] **Step 4: Create `JobSearchParams`**

Create `src/NationaleVacaturebank.Client/Builders/JobSearchParams.cs`:

```csharp
using NationaleVacaturebank.Client.Models.Enums;

namespace NationaleVacaturebank.Client.Builders;

internal sealed record JobSearchParams
{
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 10;
    public JobSort Sort { get; init; } = JobSort.Relevance;
    public string City { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Distance { get; init; } = 40;
}
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/NationaleVacaturebank.Client
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add src/NationaleVacaturebank.Client/Models/Internal/ src/NationaleVacaturebank.Client/Builders/JobSearchParams.cs
git commit -m "feat: add internal API models and JobSearchParams"
```

---

### Task 6: `IVacaturebankClient` and `VacaturebankClient`

**Files:**
- Create: `src/NationaleVacaturebank.Client/Client/IVacaturebankClient.cs`
- Create: `src/NationaleVacaturebank.Client/Client/VacaturebankClient.cs`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/Client/VacaturebankClientTests.cs`

- [ ] **Step 1: Create `IVacaturebankClient`**

Create `src/NationaleVacaturebank.Client/Client/IVacaturebankClient.cs`:

```csharp
using NationaleVacaturebank.Client.Builders;
using NationaleVacaturebank.Client.Models;

namespace NationaleVacaturebank.Client.Client;

public interface IVacaturebankClient
{
    JobSearchBuilder Jobs();
    Task<IReadOnlyList<string>> SearchFunctionTitlesAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SearchCitiesAsync(string startsWith, CancellationToken ct = default);
    Task<GeoLocation> GetGeoLocationAsync(string cityName, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/NationaleVacaturebank.Client.Tests.Unit/Client/VacaturebankClientTests.cs`:

```csharp
using System.Net;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Options;
using NationaleVacaturebank.Client.Tests.Unit.Helpers;

namespace NationaleVacaturebank.Client.Tests.Unit.Client;

public class VacaturebankClientTests
{
    private static VacaturebankClient CreateClient(string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseJson, statusCode);
        return new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());
    }

    // --- SearchFunctionTitlesAsync ---

    [Fact]
    public async Task SearchFunctionTitlesAsync_ReturnsSuggestions()
    {
        var client = CreateClient("""{"suggestions":["Software Developer","Software Engineer"]}""");

        var result = await client.SearchFunctionTitlesAsync("software");

        Assert.Equal(2, result.Count);
        Assert.Contains("Software Developer", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchFunctionTitlesAsync_ThrowsValidation_WhenQueryBlank(string query)
    {
        var client = CreateClient("{}");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.SearchFunctionTitlesAsync(query));
    }

    [Fact]
    public async Task SearchFunctionTitlesAsync_ThrowsApiException_OnNonSuccess()
    {
        var client = CreateClient("{}", HttpStatusCode.ServiceUnavailable);

        var ex = await Assert.ThrowsAsync<VacaturebankApiException>(
            () => client.SearchFunctionTitlesAsync("dev"));

        Assert.Equal(503, ex.StatusCode);
    }

    // --- SearchCitiesAsync ---

    [Fact]
    public async Task SearchCitiesAsync_ReturnsCities()
    {
        var client = CreateClient("""["Amsterdam","Amstelveen"]""");

        var result = await client.SearchCitiesAsync("Amst");

        Assert.Equal(2, result.Count);
        Assert.Contains("Amsterdam", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchCitiesAsync_ThrowsValidation_WhenStartsWithBlank(string startsWith)
    {
        var client = CreateClient("[]");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.SearchCitiesAsync(startsWith));
    }

    // --- GetGeoLocationAsync ---

    [Fact]
    public async Task GetGeoLocationAsync_ReturnsCoordinates()
    {
        var client = CreateClient("""
            {"cityCenter":{"latitude":"52.370216","longitude":"4.895168"},"cityName":"Amsterdam"}
            """);

        var result = await client.GetGeoLocationAsync("Amsterdam");

        Assert.Equal("Amsterdam", result.CityName);
        Assert.Equal(52.370216, result.Latitude, precision: 5);
        Assert.Equal(4.895168, result.Longitude, precision: 5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetGeoLocationAsync_ThrowsValidation_WhenCityNameBlank(string cityName)
    {
        var client = CreateClient("{}");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.GetGeoLocationAsync(cityName));
    }

    [Fact]
    public async Task GetGeoLocationAsync_ThrowsApiException_OnNonSuccess()
    {
        var client = CreateClient("{}", HttpStatusCode.NotFound);

        var ex = await Assert.ThrowsAsync<VacaturebankApiException>(
            () => client.GetGeoLocationAsync("UnknownCity"));

        Assert.Equal(404, ex.StatusCode);
    }
}
```

- [ ] **Step 3: Run to confirm build failure**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "VacaturebankClientTests" -v minimal
```

Expected: build error — `VacaturebankClient` not found.

- [ ] **Step 4: Create `VacaturebankClient`**

Create `src/NationaleVacaturebank.Client/Client/VacaturebankClient.cs`:

```csharp
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
        return response.Suggestions?.AsReadOnly() ?? Array.Empty<string>();
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
        Categories = m.Categories?.AsReadOnly() ?? Array.Empty<string>(),
        Industries = m.Industries?.AsReadOnly() ?? Array.Empty<string>(),
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Status = m.Status,
        WorkingHours = m.WorkingHours is { } w ? new WorkingHoursRange(w.Min, w.Max) : null,
    };
}
```

- [ ] **Step 5: Run the tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "VacaturebankClientTests" -v minimal
```

Expected: `Failed: 0, Passed: 11`.

- [ ] **Step 6: Commit**

```bash
git add src/NationaleVacaturebank.Client/Client/ tests/NationaleVacaturebank.Client.Tests.Unit/Client/
git commit -m "feat: add IVacaturebankClient and VacaturebankClient"
```

---

### Task 7: `JobSearchBuilder`

**Files:**
- Create: `src/NationaleVacaturebank.Client/Builders/JobSearchBuilder.cs`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/Builders/JobSearchBuilderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/NationaleVacaturebank.Client.Tests.Unit/Builders/JobSearchBuilderTests.cs`:

```csharp
using System.Net;
using System.Text;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Models;
using NationaleVacaturebank.Client.Models.Enums;
using NationaleVacaturebank.Client.Options;
using NationaleVacaturebank.Client.Tests.Unit.Helpers;

namespace NationaleVacaturebank.Client.Tests.Unit.Builders;

public class JobSearchBuilderTests
{
    private const string SingleJobPageJson = """
        {
          "page": 1, "limit": 10, "pages": 3, "total": 25,
          "_embedded": {
            "jobs": [
              {
                "id": "abc123", "title": "C# Developer",
                "categories": ["IT"], "industries": ["Technology"],
                "company": {"name": "Acme BV"}
              }
            ]
          },
          "_links": { "next": { "href": "https://api.nationalevacaturebank.nl/jobs?page=2" } }
        }
        """;

    private static VacaturebankClient CreateClient(string json,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(json, status);
        return new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());
    }

    [Fact]
    public async Task GetPageAsync_ReturnsJobsPage()
    {
        var client = CreateClient(SingleJobPageJson);

        var page = await client.Jobs().GetPageAsync(page: 1, limit: 10);

        Assert.Equal(1, page.Page);
        Assert.Equal(25, page.Total);
        Assert.Single(page.Jobs);
        Assert.Equal("abc123", page.Jobs[0].Id);
        Assert.Equal("C# Developer", page.Jobs[0].Title);
        Assert.Equal("Acme BV", page.Jobs[0].Company?.Name);
    }

    [Fact]
    public void HasNextPage_IsTrue_WhenNotOnLastPage()
    {
        var page = new JobsPage { Page = 1, Limit = 10, Pages = 3, Total = 25, Jobs = [] };
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void HasNextPage_IsFalse_OnLastPage()
    {
        var page = new JobsPage { Page = 3, Limit = 10, Pages = 3, Total = 25, Jobs = [] };
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPageAsync_ThrowsValidation_WhenPageInvalid(int invalidPage)
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().GetPageAsync(page: invalidPage, limit: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetPageAsync_ThrowsValidation_WhenLimitOutOfRange(int invalidLimit)
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().GetPageAsync(page: 1, limit: invalidLimit));
    }

    [Fact]
    public async Task GetPageAsync_ThrowsValidation_WhenWithinKmSetWithoutCoordinates()
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().WithinKm(10).GetPageAsync());
    }

    [Fact]
    public async Task GetPageAsync_DoesNotThrow_WhenWithinKmAndCoordinatesBothSet()
    {
        var client = CreateClient(SingleJobPageJson);

        var page = await client.Jobs()
            .AtCoordinates(52.37, 4.89)
            .WithinKm(10)
            .GetPageAsync();

        Assert.NotNull(page);
    }

    [Fact]
    public async Task GetPageAsync_SendsCorrectQueryParameters()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SingleJobPageJson, Encoding.UTF8, "application/json")
            };
        });
        var client = new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());

        await client.Jobs()
            .WithTitle("Developer")
            .InCity("Amsterdam")
            .SortBy(JobSort.Date)
            .GetPageAsync(page: 1, limit: 5);

        Assert.NotNull(captured);
        var url = Uri.UnescapeDataString(captured!.RequestUri!.ToString());
        Assert.Contains("sort=date", url);
        Assert.Contains("limit=5", url);
        Assert.Contains("dcoTitle:Developer", url);
        Assert.Contains("city:Amsterdam", url);
    }

    [Fact]
    public async Task GetAllAsync_YieldsAllJobsAcrossPages()
    {
        var page1 = """
            {
              "page": 1, "limit": 1, "pages": 2, "total": 2,
              "_embedded": { "jobs": [{"id": "job1", "title": "Job 1", "categories": [], "industries": []}] },
              "_links": { "next": { "href": "..." } }
            }
            """;
        var page2 = """
            {
              "page": 2, "limit": 1, "pages": 2, "total": 2,
              "_embedded": { "jobs": [{"id": "job2", "title": "Job 2", "categories": [], "industries": []}] },
              "_links": {}
            }
            """;
        var callCount = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            var json = ++callCount == 1 ? page1 : page2;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var client = new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());

        var jobs = new List<Job>();
        await foreach (var job in client.Jobs().GetAllAsync(limit: 1))
            jobs.Add(job);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("job1", jobs[0].Id);
        Assert.Equal("job2", jobs[1].Id);
    }
}
```

- [ ] **Step 2: Run to confirm build failure**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "JobSearchBuilderTests" -v minimal
```

Expected: build error — `JobSearchBuilder` not found.

- [ ] **Step 3: Create `JobSearchBuilder`**

Create `src/NationaleVacaturebank.Client/Builders/JobSearchBuilder.cs`:

```csharp
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
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "JobSearchBuilderTests" -v minimal
```

Expected: `Failed: 0, Passed: 11`.

- [ ] **Step 5: Run all unit tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit -v minimal
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/NationaleVacaturebank.Client/Builders/JobSearchBuilder.cs tests/NationaleVacaturebank.Client.Tests.Unit/Builders/
git commit -m "feat: add fluent JobSearchBuilder with auto-pagination"
```

---

### Task 8: DI extension

**Files:**
- Create: `src/NationaleVacaturebank.Client/Extensions/ServiceCollectionExtensions.cs`
- Create: `tests/NationaleVacaturebank.Client.Tests.Unit/Extensions/ServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Add DI package to the unit test project**

```bash
dotnet add tests/NationaleVacaturebank.Client.Tests.Unit package Microsoft.Extensions.DependencyInjection
```

- [ ] **Step 2: Write the failing tests**

Create `tests/NationaleVacaturebank.Client.Tests.Unit/Extensions/ServiceCollectionExtensionsTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Extensions;
using NationaleVacaturebank.Client.Options;

namespace NationaleVacaturebank.Client.Tests.Unit.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNationaleVacaturebank_RegistersIVacaturebankClient()
    {
        var services = new ServiceCollection();
        services.AddNationaleVacaturebank();

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IVacaturebankClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddNationaleVacaturebank_AppliesCustomOptions()
    {
        var services = new ServiceCollection();
        services.AddNationaleVacaturebank(options =>
            options.Timeout = TimeSpan.FromSeconds(5));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<VacaturebankOptions>();

        Assert.Equal(TimeSpan.FromSeconds(5), options.Timeout);
    }

    [Fact]
    public void AddNationaleVacaturebank_UsesDefaultBaseUrl_WhenNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddNationaleVacaturebank();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<VacaturebankOptions>();

        Assert.Equal("https://api.nationalevacaturebank.nl", options.BaseUrl);
    }
}
```

- [ ] **Step 3: Run to confirm build failure**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "ServiceCollectionExtensionsTests" -v minimal
```

Expected: build error — `ServiceCollectionExtensions` not found.

- [ ] **Step 4: Create `ServiceCollectionExtensions`**

Create `src/NationaleVacaturebank.Client/Extensions/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Options;

namespace NationaleVacaturebank.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNationaleVacaturebank(
        this IServiceCollection services,
        Action<VacaturebankOptions>? configure = null)
    {
        var options = new VacaturebankOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddHttpClient<IVacaturebankClient, VacaturebankClient>(client =>
        {
            client.Timeout = options.Timeout;
        });

        return services;
    }
}
```

- [ ] **Step 5: Run the tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit --filter "ServiceCollectionExtensionsTests" -v minimal
```

Expected: `Failed: 0, Passed: 3`.

- [ ] **Step 6: Run all unit tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Unit -v minimal
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/NationaleVacaturebank.Client/Extensions/ tests/NationaleVacaturebank.Client.Tests.Unit/Extensions/
git commit -m "feat: add AddNationaleVacaturebank() DI extension"
```

---

### Task 9: Integration tests

**Files:**
- Create: `tests/NationaleVacaturebank.Client.Tests.Integration/VacaturebankClientIntegrationTests.cs`

These call the live API. Run them before committing — they also serve as conformance tests that catch API schema changes.

- [ ] **Step 1: Write the integration tests**

Create `tests/NationaleVacaturebank.Client.Tests.Integration/VacaturebankClientIntegrationTests.cs`:

```csharp
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Models;
using NationaleVacaturebank.Client.Options;

namespace NationaleVacaturebank.Client.Tests.Integration;

public class VacaturebankClientIntegrationTests
{
    private static readonly VacaturebankClient Client =
        new(new HttpClient(), new VacaturebankOptions());

    [Fact]
    public async Task SearchFunctionTitlesAsync_ReturnsResults()
    {
        var results = await Client.SearchFunctionTitlesAsync("software");

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task SearchCitiesAsync_ReturnsMatchingCities()
    {
        var results = await Client.SearchCitiesAsync("Amst");

        Assert.NotEmpty(results);
        Assert.Contains(results, c => c.StartsWith("Amst", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetGeoLocationAsync_ReturnsCoordinatesForAmsterdam()
    {
        var location = await Client.GetGeoLocationAsync("Amsterdam");

        Assert.Equal("Amsterdam", location.CityName, ignoreCase: true);
        Assert.InRange(location.Latitude, 52.0, 53.0);
        Assert.InRange(location.Longitude, 4.0, 5.5);
    }

    [Fact]
    public async Task Jobs_GetPageAsync_ReturnsNonEmptyPage()
    {
        var page = await Client.Jobs().GetPageAsync(page: 1, limit: 5);

        Assert.True(page.Total > 0);
        Assert.NotEmpty(page.Jobs);
        Assert.All(page.Jobs, j => Assert.NotEmpty(j.Id));
    }

    [Fact]
    public async Task Jobs_GetAllAsync_YieldsJobsAcrossMultiplePages()
    {
        var jobs = new List<Job>();
        await foreach (var job in Client.Jobs().WithTitle("developer").GetAllAsync(limit: 10))
        {
            jobs.Add(job);
            if (jobs.Count >= 15) break; // Stop early; don't exhaust the entire dataset
        }

        Assert.True(jobs.Count >= 10);
    }
}
```

- [ ] **Step 2: Run the integration tests**

```bash
dotnet test tests/NationaleVacaturebank.Client.Tests.Integration -v minimal
```

Expected: all 5 pass. If a test fails because a field is missing or has a different shape than expected, that means the API response differs from what the Go MCP server documented. Update the relevant internal model in `src/NationaleVacaturebank.Client/Models/Internal/` and re-run.

- [ ] **Step 3: Commit**

```bash
git add tests/NationaleVacaturebank.Client.Tests.Integration/
git commit -m "test: add integration tests against live NVB API"
```

---

### Task 10: README and NuGet pack verification

**Files:**
- Create: `README.md`

- [ ] **Step 1: Create `README.md`**

Create `README.md` in the repo root:

```markdown
# NationaleVacaturebank.Client

C# SDK for the [Nationale Vacaturebank](https://www.nationalevacaturebank.nl) API — the Dutch national job board.

## Installation

```
dotnet add package NationaleVacaturebank.Client
```

## Quick start

```csharp
// Standalone
var client = new VacaturebankClient(new HttpClient(), new VacaturebankOptions());

// ASP.NET Core / Microsoft.Extensions DI
services.AddNationaleVacaturebank();
// Then inject IVacaturebankClient into your services
```

## Usage

### Search jobs (paged)

```csharp
JobsPage page = await client.Jobs()
    .WithTitle("Software Engineer")
    .InCity("Amsterdam")
    .SortBy(JobSort.Date)
    .GetPageAsync(page: 1, limit: 20);

Console.WriteLine($"Total: {page.Total}");
foreach (var job in page.Jobs)
    Console.WriteLine($"{job.Title} at {job.Company?.Name}");
```

### Iterate all results (auto-pagination)

```csharp
await foreach (var job in client.Jobs().WithTitle("Developer").GetAllAsync())
    Console.WriteLine(job.Title);
```

### Location-based search

```csharp
GeoLocation geo = await client.GetGeoLocationAsync("Rotterdam");
JobsPage page = await client.Jobs()
    .AtCoordinates(geo.Latitude, geo.Longitude)
    .WithinKm(15)
    .SortBy(JobSort.Distance)
    .GetPageAsync();
```

### Autocomplete helpers

```csharp
IReadOnlyList<string> titles = await client.SearchFunctionTitlesAsync("soft");
IReadOnlyList<string> cities = await client.SearchCitiesAsync("Den");
```

## Error handling

```csharp
try
{
    var page = await client.Jobs().GetPageAsync();
}
catch (VacaturebankApiException ex)
{
    Console.WriteLine($"HTTP {ex.StatusCode} from {ex.RequestUrl}");
}
catch (VacaturebankValidationException ex)
{
    Console.WriteLine($"Bad parameter: {ex.ParameterName} — {ex.Message}");
}
```

## License

MIT
```

- [ ] **Step 2: Build and pack**

```bash
dotnet pack src/NationaleVacaturebank.Client -c Release -o ./nupkg
```

Expected output contains: `Successfully created package './nupkg/NationaleVacaturebank.Client.1.0.0.nupkg'`

- [ ] **Step 3: Run the full test suite**

```bash
dotnet test -v minimal
```

Expected: all unit and integration tests pass.

- [ ] **Step 4: Commit**

```bash
git add README.md src/NationaleVacaturebank.Client/NationaleVacaturebank.Client.csproj
git commit -m "docs: add README and verify NuGet pack"
```

---

*Sources used during API research:*
- *[nationalevacaturebank-mcp-server (GitHub)](https://github.com/voyagen/nationalevacaturebank-mcp-server) — Go client revealing exact endpoint paths, query parameter format, and response shapes*
