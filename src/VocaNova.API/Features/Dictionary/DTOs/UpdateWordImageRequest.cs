using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record UpdateWordImageRequest(
    [property: JsonPropertyName("image_url")] string? ImageUrl);
