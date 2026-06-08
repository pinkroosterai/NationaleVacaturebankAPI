using NationaleVacaturebank.Client.Builders;
using NationaleVacaturebank.Client.Models;

namespace NationaleVacaturebank.Client.Client;

public interface IVacaturebankClient
{
    JobSearchBuilder Jobs();
    Task<IReadOnlyList<string>> SearchFunctionTitlesAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SearchCitiesAsync(string startsWith, CancellationToken ct = default);
    Task<GeoLocation> GetGeoLocationAsync(string cityName, CancellationToken ct = default);
}
