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
