using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Extensions;

var host = Host.CreateDefaultBuilder(args)
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

    Console.WriteLine($"\nFound {page.Total} jobs. Showing first {page.Jobs.Count}:");
    foreach (var job in page.Jobs)
    {
        var company = job.Company?.Name ?? "(no company)";
        Console.WriteLine($"  {job.Title} @ {company}");
    }
}
catch (VacaturebankValidationException ex)
{
    Console.WriteLine($"Validation error ({ex.ParameterName}): {ex.Message}");
}
catch (VacaturebankApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
