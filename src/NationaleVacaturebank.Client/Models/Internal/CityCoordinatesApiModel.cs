namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class CityCoordinatesApiModel
{
    public string? Latitude { get; init; }   // API returns coordinates as strings, not numbers
    public string? Longitude { get; init; }
}