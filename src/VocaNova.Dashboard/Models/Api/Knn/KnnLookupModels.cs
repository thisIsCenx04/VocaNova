using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Knn;

// Mirror các DTO KNN lookup của VocaNova.API (F050) dùng cho F063.

public sealed record AgeRange(
    [property: JsonPropertyName("age_range_id")] uint AgeRangeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record Region(
    [property: JsonPropertyName("region_id")] uint RegionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId,
    [property: JsonPropertyName("parent_name")] string? ParentName,
    [property: JsonPropertyName("status")] string Status);

public sealed record Occupation(
    [property: JsonPropertyName("occupation_id")] uint OccupationId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);

public sealed record EducationLevel(
    [property: JsonPropertyName("education_level_id")] uint EducationLevelId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record LearningPurpose(
    [property: JsonPropertyName("learning_purpose_id")] uint LearningPurposeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);

public sealed record KnnConfig(
    [property: JsonPropertyName("onboarding")] KnnOnboardingConfig Onboarding,
    [property: JsonPropertyName("learning")] KnnLearningConfig Learning);

public sealed record KnnOnboardingConfig(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("default_topic_limit")] int DefaultTopicLimit,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record KnnLearningConfig(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("min_sessions")] int MinSessions,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("recommendation_count")] int RecommendationCount,
    [property: JsonPropertyName("rebuild_interval_hours")] int RebuildIntervalHours,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record KnnRebuildStatus(
    [property: JsonPropertyName("last_rebuild_at")] DateTime? LastRebuildAt,
    [property: JsonPropertyName("is_running")] bool IsRunning);
