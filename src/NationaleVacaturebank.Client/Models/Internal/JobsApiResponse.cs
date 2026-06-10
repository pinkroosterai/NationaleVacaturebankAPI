using System.Text.Json.Serialization;

namespace NationaleVacaturebank.Client.Models.Internal;

internal sealed class JobsApiResponse
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Pages { get; init; }
    public int Total { get; init; }

    [JsonPropertyName("_links")]
    public LinksApiModel? Links { get; init; }

    [JsonPropertyName("_embedded")]
    public EmbeddedApiModel? Embedded { get; init; }
}