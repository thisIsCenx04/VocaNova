namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnLearningProfileDto(
    uint UserId,
    uint? AgeRangeId,
    string? AgeRangeName,
    uint? RegionId,
    string? RegionName,
    uint? OccupationId,
    string? OccupationName,
    uint? EducationLevelId,
    string? EducationLevelName,
    uint? LearningPurposeId,
    string? LearningPurposeName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
