namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class GeoLocationApiModel
{
    public CityCoordinatesApiModel? CityCenter { get; init; }
    public string? CityName { get; init; }
}

internal sealed class CityCoordinatesApiModel
{
    public string? Latitude { get; init; }   // API returns coordinates as strings, not numbers
    public string? Longitude { get; init; }
}
