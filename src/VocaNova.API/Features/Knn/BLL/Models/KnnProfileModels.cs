namespace VocaNova.API.Features.Knn.BLL.Models;

public sealed record LearningProfileOption(uint Id, string Name);

public sealed record LearningProfileOptions(
    IReadOnlyList<LearningProfileOption> AgeRanges,
    IReadOnlyList<LearningProfileOption> Regions,
    IReadOnlyList<LearningProfileOption> Occupations,
    IReadOnlyList<LearningProfileOption> EducationLevels,
    IReadOnlyList<LearningProfileOption> LearningPurposes);

public sealed record KnnLearningProfile(
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

public sealed record KnnLookupDimensions(
    IReadOnlyList<uint> AgeRangeIds,
    IReadOnlyList<uint> RegionIds,
    IReadOnlyList<uint> OccupationIds,
    IReadOnlyList<uint> EducationLevelIds,
    IReadOnlyList<uint> LearningPurposeIds,
    IReadOnlyList<uint> TopicIds);

public sealed record KnnProfileVectorSource(
    uint UserId,
    uint? AgeRangeId,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId,
    uint? LearningPurposeId,
    IReadOnlyCollection<uint>? InterestTopicIds = null);

public sealed record KnnTopicPreference(
    uint UserId,
    uint TopicId,
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    string Source,
    string Status,
    DateTime CreatedAt);
