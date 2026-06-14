using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.DTOs;

public sealed record AdminUserQuery(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("search")] string? Search = null);

public sealed record AdminUserSummaryDto(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record AdminUserDetailDto(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("google_email")] string? GoogleEmail,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("learning_profile")] AdminUserLearningProfileDto? LearningProfile);

public sealed record AdminUserLearningProfileDto(
    [property: JsonPropertyName("age_range_id")] uint? AgeRangeId,
    [property: JsonPropertyName("age_range_name")] string? AgeRangeName,
    [property: JsonPropertyName("region_id")] uint? RegionId,
    [property: JsonPropertyName("region_name")] string? RegionName,
    [property: JsonPropertyName("occupation_id")] uint? OccupationId,
    [property: JsonPropertyName("occupation_name")] string? OccupationName,
    [property: JsonPropertyName("education_level_id")] uint? EducationLevelId,
    [property: JsonPropertyName("education_level_name")] string? EducationLevelName,
    [property: JsonPropertyName("learning_purpose_id")] uint? LearningPurposeId,
    [property: JsonPropertyName("learning_purpose_name")] string? LearningPurposeName);
