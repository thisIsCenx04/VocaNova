namespace VocaNova.API.Features.Admin.BLL.Models;

public sealed record AdminDashboardStatsModel(
    int TotalUsers,
    int TotalWords,
    int SessionsToday,
    double AvgAccuracy7d);

public sealed record AdminDemographicsModel(
    IReadOnlyCollection<AdminDemographicGroupModel> AgeRanges,
    IReadOnlyCollection<AdminDemographicGroupModel> Occupations,
    IReadOnlyCollection<AdminDemographicGroupModel> EducationLevels);

public sealed record AdminDemographicGroupModel(
    uint Id,
    string Name,
    int UserCount);

public sealed record AdminLearningStatsModel(
    IReadOnlyCollection<AdminWrongWordModel> TopWrongWords,
    IReadOnlyCollection<AdminAccuracyTrendPointModel> AccuracyTrend);

public sealed record AdminWrongWordModel(
    uint WordId,
    string Word,
    int WrongCount,
    int CorrectCount,
    int TotalCount,
    double Accuracy);

public sealed record AdminAccuracyTrendPointModel(
    DateOnly Date,
    int CorrectCount,
    int WrongCount,
    int TotalCount,
    double Accuracy);

public sealed record AdminAuditLogQuery(
    int Page = 1,
    int Limit = 20,
    uint? UserId = null,
    string? Entity = null);

public sealed record AdminAuditLogModel(
    uint LogId,
    uint UserId,
    string Action,
    string EntityType,
    uint? EntityId,
    string? PayloadBefore,
    string? PayloadAfter,
    string? IpAddress,
    DateTime CreatedAt);

public sealed record AdminSessionAccuracyRow(
    DateOnly Date,
    int CorrectCount,
    int WrongCount);

public sealed record AdminSessionsTrendModel(
    int Days,
    IReadOnlyCollection<AdminSessionTrendPointModel> Points);

public sealed record AdminSessionTrendPointModel(
    DateOnly Date,
    int SessionCount);

public sealed record AdminSessionCountRow(
    DateOnly Date,
    int SessionCount);

public sealed record AdminMasteryDistributionModel(
    int TotalWordsInProgress,
    IReadOnlyCollection<AdminMasteryLevelModel> Levels);

public sealed record AdminMasteryLevelModel(
    int Level,
    int WordCount);

public sealed record AdminMasteryCountRow(
    int Level,
    int Count);

public sealed record AdminActivityTrendModel(
    string Granularity,
    IReadOnlyCollection<AdminActivityTrendPointModel> Points);

public sealed record AdminActivityTrendPointModel(
    string Period,
    int SessionsCount,
    int CorrectCount,
    int TotalCount,
    double Accuracy);
