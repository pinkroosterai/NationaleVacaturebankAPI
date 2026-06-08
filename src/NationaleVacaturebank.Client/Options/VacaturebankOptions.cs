namespace NationaleVacaturebank.Client.Options;

public sealed class VacaturebankOptions
{
    public string BaseUrl { get; set; } = "https://api.nationalevacaturebank.nl";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
