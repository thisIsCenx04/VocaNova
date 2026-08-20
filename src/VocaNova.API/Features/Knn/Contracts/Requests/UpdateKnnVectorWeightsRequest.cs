using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed record UpdateKnnVectorWeightsRequest(
    [property: JsonPropertyName("age_range_weight")] double? AgeRangeWeight,
    [property: JsonPropertyName("region_weight")] double? RegionWeight,
    [property: JsonPropertyName("occupation_weight")] double? OccupationWeight,
    [property: JsonPropertyName("education_level_weight")] double? EducationLevelWeight,
    [property: JsonPropertyName("learning_purpose_weight")] double? LearningPurposeWeight,
    [property: JsonPropertyName("interest_topics_weight")] double? InterestTopicsWeight);
