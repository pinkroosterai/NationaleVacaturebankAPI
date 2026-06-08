using NationaleVacaturebank.Client.Exceptions;

namespace NationaleVacaturebank.Client.Tests.Unit;

public class ExceptionTests
{
    [Fact]
    public void VacaturebankApiException_StoresStatusCodeAndUrl()
    {
        var ex = new VacaturebankApiException(404, "https://example.com/api");

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("https://example.com/api", ex.RequestUrl);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void VacaturebankApiException_UsesCustomMessage_WhenProvided()
    {
        var ex = new VacaturebankApiException(500, "https://example.com/api", "Custom error");

        Assert.Equal("Custom error", ex.Message);
    }

    [Fact]
    public void VacaturebankValidationException_StoresParameterName()
    {
        var ex = new VacaturebankValidationException("query", "query cannot be empty.");

        Assert.Equal("query", ex.ParameterName);
        Assert.Equal("query cannot be empty.", ex.Message);
    }
}
