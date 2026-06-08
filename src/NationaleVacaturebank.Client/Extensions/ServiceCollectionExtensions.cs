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
