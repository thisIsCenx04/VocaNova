using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record LearningProfileOptionResponse(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string Name);

public sealed record LearningProfileOptionsResponse(
    [property: JsonPropertyName("age_ranges")] IReadOnlyList<LearningProfileOptionResponse> AgeRanges,
    [property: JsonPropertyName("regions")] IReadOnlyList<LearningProfileOptionResponse> Regions,
    [property: JsonPropertyName("occupations")] IReadOnlyList<LearningProfileOptionResponse> Occupations,
    [property: JsonPropertyName("education_levels")] IReadOnlyList<LearningProfileOptionResponse> EducationLevels,
    [property: JsonPropertyName("learning_purposes")] IReadOnlyList<LearningProfileOptionResponse> LearningPurposes);
