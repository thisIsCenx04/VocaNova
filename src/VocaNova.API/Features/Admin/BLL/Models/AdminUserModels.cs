namespace VocaNova.API.Features.Admin.BLL.Models;

public sealed record AdminUserQuery(
    int Page = 1,
    int Limit = 20,
    string? Status = null,
    string? Search = null,
    bool IncludeDeleted = false,
    string? Role = null,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record AdminUserSummaryModel(
    uint UserId,
    string? Phone,
    string? GoogleEmail,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string Status,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public sealed record AdminUserTopicsModel(
    IReadOnlyCollection<AdminTopicChipModel> Selected,
    IReadOnlyCollection<AdminTopicChipModel> Suggested);

public sealed record AdminTopicChipModel(
    uint TopicId,
    string Name,
    string? NameVi);

public sealed record AdminUserDetailModel(
    uint UserId,
    string? Phone,
    string? GoogleEmail,
    string? Username,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string Status,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AdminUserLearningProfileModel? LearningProfile);

public sealed record AdminUserTestSessionModel(
    uint SessionId,
    string AnswerMethod,
    string Mode,
    int QuestionType,
    int QuestionCount,
    int CorrectCount,
    int WrongCount,
    float Accuracy,
    float Score,
    int MaxStreak,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt);

public sealed record AdminUserLearningProfileModel(
    uint? AgeRangeId,
    string? AgeRangeName,
    uint? RegionId,
    string? RegionName,
    uint? OccupationId,
    string? OccupationName,
    uint? EducationLevelId,
    string? EducationLevelName,
    uint? LearningPurposeId,
    string? LearningPurposeName);

public sealed record AdminUserStatusTarget(
    uint UserId,
    string Status,
    string? RoleName);
