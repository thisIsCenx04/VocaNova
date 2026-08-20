namespace VocaNova.API.Features.Knn.BLL.Models;

public sealed record KnnLookupQuery(
    int Page = 1,
    int Limit = 20,
    string? Q = null,
    string? Status = null,
    bool IncludeDeleted = false,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record AgeRangeLookup(
    uint AgeRangeId,
    string Name,
    int? MinAge,
    int? MaxAge,
    int DisplayOrder,
    string Status);

public sealed record SaveAgeRangeCommand(string? Name, int? MinAge, int? MaxAge, int DisplayOrder);

public sealed record RegionLookup(
    uint RegionId,
    string Name,
    string Code,
    uint? ParentId,
    string? ParentName,
    string Status);

public sealed record SaveRegionCommand(string? Name, string? Code, uint? ParentId);

public sealed record OccupationLookup(uint OccupationId, string Name, string? Description, string Status);

public sealed record SaveOccupationCommand(string? Name, string? Description);

public sealed record EducationLevelLookup(
    uint EducationLevelId,
    string Name,
    string? Description,
    int DisplayOrder,
    string Status);

public sealed record SaveEducationLevelCommand(string? Name, string? Description, int DisplayOrder);

public sealed record LearningPurposeLookup(
    uint LearningPurposeId,
    string Name,
    string? Description,
    string Status);

public sealed record SaveLearningPurposeCommand(string? Name, string? Description);

public sealed record KnnConfig(
    KnnOnboardingConfig Onboarding,
    KnnLearningConfig Learning,
    KnnVectorConfig Vector);

public sealed record KnnOnboardingConfig(int KValue, int DefaultTopicLimit, double MinSimilarity, int CacheTtlMinutes);

public sealed record KnnLearningConfig(
    int KValue,
    int MinSessions,
    double MinSimilarity,
    int RecommendationCount,
    int RebuildIntervalHours,
    int CacheTtlMinutes);

public sealed record KnnVectorWeights(
    double AgeRangeWeight,
    double RegionWeight,
    double OccupationWeight,
    double EducationLevelWeight,
    double LearningPurposeWeight,
    double InterestTopicsWeight);

public sealed record KnnVectorConfig(
    KnnVectorWeights Weights,
    KnnVectorWeights Defaults,
    bool IsOverridden,
    string Storage,
    bool CanWriteEnvFile);

public sealed record TriggerKnnRebuildResult(string Message, DateTime TriggeredAt);
