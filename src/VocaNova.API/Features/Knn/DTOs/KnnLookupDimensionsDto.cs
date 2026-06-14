namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnLookupDimensionsDto(
    IReadOnlyList<uint> AgeRangeIds,
    IReadOnlyList<uint> RegionIds,
    IReadOnlyList<uint> OccupationIds,
    IReadOnlyList<uint> EducationLevelIds,
    IReadOnlyList<uint> LearningPurposeIds);
