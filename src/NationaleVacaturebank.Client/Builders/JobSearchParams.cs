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
