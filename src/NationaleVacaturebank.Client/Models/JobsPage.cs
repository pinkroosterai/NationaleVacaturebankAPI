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
