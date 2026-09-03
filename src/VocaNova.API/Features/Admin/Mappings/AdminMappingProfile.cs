using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Features.Admin.Contracts.Responses;

namespace VocaNova.API.Features.Admin.Mappings;

public static class AdminMappingProfile
{
    public static AdminDashboardStatsResponse ToResponse(this AdminDashboardStatsModel model) =>
        new(model.TotalUsers, model.TotalWords, model.SessionsToday, model.AvgAccuracy7d);

    public static AdminDemographicsResponse ToResponse(this AdminDemographicsModel model) =>
        new(
            model.AgeRanges.Select(ToResponse).ToArray(),
            model.Occupations.Select(ToResponse).ToArray(),
            model.EducationLevels.Select(ToResponse).ToArray());

    public static AdminDemographicGroupResponse ToResponse(this AdminDemographicGroupModel model) =>
        new(model.Id, model.Name, model.UserCount);

    public static AdminLearningStatsResponse ToResponse(this AdminLearningStatsModel model) =>
        new(
            model.TopWrongWords.Select(ToResponse).ToArray(),
            model.AccuracyTrend.Select(ToResponse).ToArray());

    public static AdminWrongWordResponse ToResponse(this AdminWrongWordModel model) =>
        new(model.WordId, model.Word, model.WrongCount, model.CorrectCount, model.TotalCount, model.Accuracy);

    public static AdminAccuracyTrendPointResponse ToResponse(this AdminAccuracyTrendPointModel model) =>
        new(model.Date, model.CorrectCount, model.WrongCount, model.TotalCount, model.Accuracy);

    public static AdminSessionsTrendResponse ToResponse(this AdminSessionsTrendModel model) =>
        new(model.Days, model.Points.Select(ToResponse).ToArray());

    public static AdminSessionTrendPointResponse ToResponse(this AdminSessionTrendPointModel model) =>
        new(model.Date, model.SessionCount);

    public static AdminMasteryDistributionResponse ToResponse(this AdminMasteryDistributionModel model) =>
        new(model.TotalWordsInProgress, model.Levels.Select(ToResponse).ToArray());

    public static AdminMasteryLevelResponse ToResponse(this AdminMasteryLevelModel model) =>
        new(model.Level, model.WordCount);

    public static AdminActivityTrendResponse ToResponse(this AdminActivityTrendModel model) =>
        new(model.Granularity, model.Points.Select(ToResponse).ToArray());

    public static AdminActivityTrendPointResponse ToResponse(this AdminActivityTrendPointModel model) =>
        new(model.Period, model.SessionsCount, model.CorrectCount, model.TotalCount, model.Accuracy);

    public static AdminAuditLogResponse ToResponse(this AdminAuditLogModel model) =>
        new(model.LogId, model.UserId, model.Action, model.EntityType, model.EntityId, model.PayloadBefore, model.PayloadAfter, model.IpAddress, model.CreatedAt);

    public static AdminUserSummaryResponse ToResponse(this AdminUserSummaryModel model) =>
        new(model.UserId, model.Phone, model.GoogleEmail, model.DisplayName, model.AvatarUrl, model.Role, model.Status, model.LastLoginAt, model.CreatedAt);

    public static AdminUserDetailResponse ToResponse(this AdminUserDetailModel model) =>
        new(model.UserId, model.Phone, model.GoogleEmail, model.Username, model.DisplayName, model.AvatarUrl, model.Role, model.Status, model.LastLoginAt, model.CreatedAt, model.UpdatedAt, model.LearningProfile?.ToResponse());

    public static AdminUserLearningProfileResponse ToResponse(this AdminUserLearningProfileModel model) =>
        new(model.AgeRangeId, model.AgeRangeName, model.RegionId, model.RegionName, model.OccupationId, model.OccupationName, model.EducationLevelId, model.EducationLevelName, model.LearningPurposeId, model.LearningPurposeName);

    public static AdminUserTestSessionResponse ToResponse(this AdminUserTestSessionModel model) =>
        new(model.SessionId, model.AnswerMethod, model.Mode, model.QuestionType, model.QuestionCount, model.CorrectCount, model.WrongCount, model.Accuracy, model.Score, model.MaxStreak, model.Status, model.StartedAt, model.EndedAt);

    public static AdminUserTopicsResponse ToResponse(this AdminUserTopicsModel model) =>
        new(model.Selected.Select(ToResponse).ToArray(), model.Suggested.Select(ToResponse).ToArray());

    public static AdminTopicChipResponse ToResponse(this AdminTopicChipModel model) =>
        new(model.TopicId, model.Name, model.NameVi);
}
