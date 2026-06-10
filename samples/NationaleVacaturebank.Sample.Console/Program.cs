using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Extensions;
using Spectre.Console;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging.ClearProviders())
    .ConfigureServices(services => services.AddNationaleVacaturebank())
    .Build();

var client = host.Services.GetRequiredService<IVacaturebankClient>();

Console.Write("Enter job title: ");
var title = Console.ReadLine() ?? string.Empty;

Console.Write("Enter city (leave blank to skip): ");
var city = Console.ReadLine() ?? string.Empty;

try
{
    var builder = client.Jobs().WithTitle(title);
    if (!string.IsNullOrWhiteSpace(city))
        builder = builder.InCity(city);

    var page = await builder.GetPageAsync(limit: 10);

    if (page.Total == 0)
    {
        Console.WriteLine("No jobs found.");
        return;
    }

    Console.WriteLine($"\nFound {page.Total} jobs. Showing {page.Jobs.Count} result(s):\n");

    var table = new Table();
    table.AddColumn("Title");
    table.AddColumn("Company");
    table.AddColumn("Salary");
    table.AddColumn("Contract Type");
    table.AddColumn("Career Level");

    foreach (var job in page.Jobs)
    {
        var salary = job.Salary is { } s ? $"€{s.Min:N0}–€{s.Max:N0}" : "—";
        table.AddRow(
            new Text(job.Title),
            new Text(job.Company?.Name ?? "—"),
            new Text(salary),
            new Text(job.ContractType ?? "—"),
            new Text(job.CareerLevel ?? "—")
        );
    }

    AnsiConsole.Write(table);
}
catch (VacaturebankValidationException ex)
{
    Console.WriteLine($"Validation error ({ex.ParameterName}): {ex.Message}");
}
catch (VacaturebankApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
