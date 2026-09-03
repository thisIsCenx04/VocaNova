using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record WordAudioResponse(
    [property: JsonPropertyName("audio_id")] uint AudioId,
    [property: JsonPropertyName("accent")] string Accent,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("status")] string Status);
