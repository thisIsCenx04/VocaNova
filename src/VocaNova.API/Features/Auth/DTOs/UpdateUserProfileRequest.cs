using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record UpdateUserProfileRequest(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl);
