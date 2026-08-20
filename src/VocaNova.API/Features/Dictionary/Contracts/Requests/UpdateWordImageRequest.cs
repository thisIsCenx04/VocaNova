using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed record UpdateWordImageRequest(
    [property: JsonPropertyName("image_url")] string? ImageUrl);
