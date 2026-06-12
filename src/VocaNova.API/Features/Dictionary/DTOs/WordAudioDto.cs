using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordAudioDto(
    [property: JsonPropertyName("audio_id")] uint AudioId,
    [property: JsonPropertyName("accent")] string Accent,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("status")] string Status);
