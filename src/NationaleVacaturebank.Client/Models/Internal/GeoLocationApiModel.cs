namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class GeoLocationApiModel
{
    public CityCoordinatesApiModel? CityCenter { get; init; }
    public string? CityName { get; init; }
}