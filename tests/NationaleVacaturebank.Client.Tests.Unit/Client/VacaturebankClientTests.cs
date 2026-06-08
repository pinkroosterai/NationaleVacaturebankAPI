using System.Net;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Options;
using NationaleVacaturebank.Client.Tests.Unit.Helpers;

namespace NationaleVacaturebank.Client.Tests.Unit.Client;

public class VacaturebankClientTests
{
    private static VacaturebankClient CreateClient(string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseJson, statusCode);
        return new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());
    }

    // --- SearchFunctionTitlesAsync ---

    [Fact]
    public async Task SearchFunctionTitlesAsync_ReturnsSuggestions()
    {
        var client = CreateClient("""{"suggestions":["Software Developer","Software Engineer"]}""");

        var result = await client.SearchFunctionTitlesAsync("software");

        Assert.Equal(2, result.Count);
        Assert.Contains("Software Developer", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchFunctionTitlesAsync_ThrowsValidation_WhenQueryBlank(string query)
    {
        var client = CreateClient("{}");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.SearchFunctionTitlesAsync(query));
    }

    [Fact]
    public async Task SearchFunctionTitlesAsync_ThrowsApiException_OnNonSuccess()
    {
        var client = CreateClient("{}", HttpStatusCode.ServiceUnavailable);

        var ex = await Assert.ThrowsAsync<VacaturebankApiException>(
            () => client.SearchFunctionTitlesAsync("dev"));

        Assert.Equal(503, ex.StatusCode);
    }

    // --- SearchCitiesAsync ---

    [Fact]
    public async Task SearchCitiesAsync_ReturnsCities()
    {
        var client = CreateClient("""["Amsterdam","Amstelveen"]""");

        var result = await client.SearchCitiesAsync("Amst");

        Assert.Equal(2, result.Count);
        Assert.Contains("Amsterdam", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchCitiesAsync_ThrowsValidation_WhenStartsWithBlank(string startsWith)
    {
        var client = CreateClient("[]");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.SearchCitiesAsync(startsWith));
    }

    // --- GetGeoLocationAsync ---

    [Fact]
    public async Task GetGeoLocationAsync_ReturnsCoordinates()
    {
        var client = CreateClient("""
            {"cityCenter":{"latitude":"52.370216","longitude":"4.895168"},"cityName":"Amsterdam"}
            """);

        var result = await client.GetGeoLocationAsync("Amsterdam");

        Assert.Equal("Amsterdam", result.CityName);
        Assert.Equal(52.370216, result.Latitude, precision: 5);
        Assert.Equal(4.895168, result.Longitude, precision: 5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetGeoLocationAsync_ThrowsValidation_WhenCityNameBlank(string cityName)
    {
        var client = CreateClient("{}");

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.GetGeoLocationAsync(cityName));
    }

    [Fact]
    public async Task GetGeoLocationAsync_ThrowsApiException_OnNonSuccess()
    {
        var client = CreateClient("{}", HttpStatusCode.NotFound);

        var ex = await Assert.ThrowsAsync<VacaturebankApiException>(
            () => client.GetGeoLocationAsync("UnknownCity"));

        Assert.Equal(404, ex.StatusCode);
    }
}
