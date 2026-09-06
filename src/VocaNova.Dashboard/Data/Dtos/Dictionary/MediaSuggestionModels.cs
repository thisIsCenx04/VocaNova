using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.Dictionary;

public sealed record MediaSuggestion(
    [property: JsonPropertyName("media_type")] string MediaType,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("external_id")] string ExternalId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("preview_url")] string PreviewUrl,
    [property: JsonPropertyName("full_size_url")] string? FullSizeUrl,
    [property: JsonPropertyName("source_url")] string SourceUrl,
    [property: JsonPropertyName("creator_name")] string? CreatorName,
    [property: JsonPropertyName("creator_url")] string? CreatorUrl,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("duration_seconds")] int? DurationSeconds);
