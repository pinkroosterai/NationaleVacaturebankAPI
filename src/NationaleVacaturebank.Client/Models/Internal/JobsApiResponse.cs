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
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
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
