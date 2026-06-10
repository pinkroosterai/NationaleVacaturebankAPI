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
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? Status { get; init; }
    public WorkingHoursRange? WorkingHours { get; init; }
}