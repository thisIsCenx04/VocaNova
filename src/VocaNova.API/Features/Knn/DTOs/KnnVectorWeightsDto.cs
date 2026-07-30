using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.DTOs;

/// <summary>
/// Per-block weights of the hybrid profile vector. The first four come from the sign-up form,
/// the last two from the onboarding questions.
/// </summary>
public sealed record KnnVectorWeightsDto(
    [property: JsonPropertyName("age_range_weight")] double AgeRangeWeight,
    [property: JsonPropertyName("region_weight")] double RegionWeight,
    [property: JsonPropertyName("occupation_weight")] double OccupationWeight,
    [property: JsonPropertyName("education_level_weight")] double EducationLevelWeight,
    [property: JsonPropertyName("learning_purpose_weight")] double LearningPurposeWeight,
    [property: JsonPropertyName("interest_topics_weight")] double InterestTopicsWeight);

public sealed record KnnVectorConfigDto(
    [property: JsonPropertyName("weights")] KnnVectorWeightsDto Weights,
    [property: JsonPropertyName("defaults")] KnnVectorWeightsDto Defaults,
    [property: JsonPropertyName("is_overridden")] bool IsOverridden,
    /// <summary><c>env_file</c> when the weights live in .env, <c>fallback</c> when the file
    /// could not be written and they are held in the shared store instead.</summary>
    [property: JsonPropertyName("storage")] string Storage,
    [property: JsonPropertyName("can_write_env_file")] bool CanWriteEnvFile);
