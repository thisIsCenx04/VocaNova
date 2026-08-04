using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.DTOs;

public sealed record LearningProfileOptionDto(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string Name);

/// <summary>
/// Public catalog backing the sign-up form and the onboarding questions, so the mobile client
/// no longer has to hard-code lookup ids.
/// </summary>
public sealed record LearningProfileOptionsDto(
    [property: JsonPropertyName("age_ranges")] IReadOnlyList<LearningProfileOptionDto> AgeRanges,
    [property: JsonPropertyName("regions")] IReadOnlyList<LearningProfileOptionDto> Regions,
    [property: JsonPropertyName("occupations")] IReadOnlyList<LearningProfileOptionDto> Occupations,
    [property: JsonPropertyName("education_levels")] IReadOnlyList<LearningProfileOptionDto> EducationLevels,
    [property: JsonPropertyName("learning_purposes")] IReadOnlyList<LearningProfileOptionDto> LearningPurposes);
