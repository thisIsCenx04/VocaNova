using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record KnnConfigResponse(
    [property: JsonPropertyName("onboarding")] KnnOnboardingConfigResponse Onboarding,
    [property: JsonPropertyName("learning")] KnnLearningConfigResponse Learning,
    [property: JsonPropertyName("vector")] KnnVectorConfigResponse Vector);

public sealed record KnnOnboardingConfigResponse(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("default_topic_limit")] int DefaultTopicLimit,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record KnnLearningConfigResponse(
    [property: JsonPropertyName("k_value")] int KValue,
    [property: JsonPropertyName("min_sessions")] int MinSessions,
    [property: JsonPropertyName("min_similarity")] double MinSimilarity,
    [property: JsonPropertyName("recommendation_count")] int RecommendationCount,
    [property: JsonPropertyName("rebuild_interval_hours")] int RebuildIntervalHours,
    [property: JsonPropertyName("cache_ttl_minutes")] int CacheTtlMinutes);

public sealed record KnnVectorWeightsResponse(
    [property: JsonPropertyName("age_range_weight")] double AgeRangeWeight,
    [property: JsonPropertyName("region_weight")] double RegionWeight,
    [property: JsonPropertyName("occupation_weight")] double OccupationWeight,
    [property: JsonPropertyName("education_level_weight")] double EducationLevelWeight,
    [property: JsonPropertyName("learning_purpose_weight")] double LearningPurposeWeight,
    [property: JsonPropertyName("interest_topics_weight")] double InterestTopicsWeight);

public sealed record KnnVectorConfigResponse(
    [property: JsonPropertyName("weights")] KnnVectorWeightsResponse Weights,
    [property: JsonPropertyName("defaults")] KnnVectorWeightsResponse Defaults,
    [property: JsonPropertyName("is_overridden")] bool IsOverridden,
    [property: JsonPropertyName("storage")] string Storage,
    [property: JsonPropertyName("can_write_env_file")] bool CanWriteEnvFile);
