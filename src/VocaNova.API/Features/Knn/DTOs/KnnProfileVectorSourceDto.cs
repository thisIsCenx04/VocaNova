namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnProfileVectorSourceDto(
    uint UserId,
    uint? AgeRangeId,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId,
    uint? LearningPurposeId);
