using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.DTOs;

public sealed record KnnLookupQuery(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("q")] string? Q = null,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("include_deleted")] bool IncludeDeleted = false);

public sealed record AgeRangeDto(
    [property: JsonPropertyName("age_range_id")] uint AgeRangeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateAgeRangeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record UpdateAgeRangeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record RegionDto(
    [property: JsonPropertyName("region_id")] uint RegionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId,
    [property: JsonPropertyName("parent_name")] string? ParentName,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateRegionRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId);

public sealed record UpdateRegionRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId);

public sealed record OccupationDto(
    [property: JsonPropertyName("occupation_id")] uint OccupationId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateOccupationRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record UpdateOccupationRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record EducationLevelDto(
    [property: JsonPropertyName("education_level_id")] uint EducationLevelId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateEducationLevelRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record UpdateEducationLevelRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record LearningPurposeDto(
    [property: JsonPropertyName("learning_purpose_id")] uint LearningPurposeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateLearningPurposeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record UpdateLearningPurposeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record KnnConfigDto(
    [property: JsonPropertyName("onboarding")] KnnOnboardingConfigDto Onboarding,
    [property: JsonPropertyName("learning")] KnnLearningConfigDto Learning);

public sealed record KnnOnboardingConfigDto(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("default_topic_limit")] int DefaultTopicLimit,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record KnnLearningConfigDto(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("min_sessions")] int MinSessions,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("recommendation_count")] int RecommendationCount,
    [property: JsonPropertyName("rebuild_interval_hours")] int RebuildIntervalHours,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record TriggerKnnRebuildResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("triggered_at")] DateTime TriggeredAt);
