namespace NationaleVacaturebank.Client.Models.Internal;

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