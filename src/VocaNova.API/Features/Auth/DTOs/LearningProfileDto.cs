using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record LearningProfileDto(
    [property: JsonPropertyName("age_range_id")] uint? AgeRangeId,
    [property: JsonPropertyName("region_id")] uint? RegionId,
    [property: JsonPropertyName("occupation_id")] uint? OccupationId,
    [property: JsonPropertyName("education_level_id")] uint? EducationLevelId,
    [property: JsonPropertyName("learning_purpose_id")] uint? LearningPurposeId);
