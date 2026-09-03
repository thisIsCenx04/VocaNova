using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Responses;

public sealed record UserProfileResponse(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("display_name")] string FullName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("learning_profile")] LearningProfileResponse? LearningProfile);
