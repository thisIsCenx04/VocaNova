using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.Users;

// Mirror các DTO admin user của VocaNova.API (F053 + test-history F060).

public sealed record AdminUserSummary(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("google_email")] string? GoogleEmail,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

// F060 redesign — Topic of Interest.
public sealed record AdminUserTopics(
    [property: JsonPropertyName("selected")] IReadOnlyList<AdminTopicChip> Selected,
    [property: JsonPropertyName("suggested")] IReadOnlyList<AdminTopicChip> Suggested);

public sealed record AdminTopicChip(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi);

public sealed record AdminUserDetail(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("google_email")] string? GoogleEmail,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("learning_profile")] AdminUserLearningProfile? LearningProfile);

public sealed record AdminUserLearningProfile(
    [property: JsonPropertyName("age_range_id")] uint? AgeRangeId,
    [property: JsonPropertyName("age_range_name")] string? AgeRangeName,
    [property: JsonPropertyName("region_id")] uint? RegionId,
    [property: JsonPropertyName("region_name")] string? RegionName,
    [property: JsonPropertyName("occupation_id")] uint? OccupationId,
    [property: JsonPropertyName("occupation_name")] string? OccupationName,
    [property: JsonPropertyName("education_level_id")] uint? EducationLevelId,
    [property: JsonPropertyName("education_level_name")] string? EducationLevelName,
    [property: JsonPropertyName("learning_purpose_id")] uint? LearningPurposeId,
    [property: JsonPropertyName("learning_purpose_name")] string? LearningPurposeName);

public sealed record AdminUserTestSession(
    [property: JsonPropertyName("session_id")] uint SessionId,
    [property: JsonPropertyName("answer_method")] string AnswerMethod,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("question_type")] int QuestionType,
    [property: JsonPropertyName("question_count")] int QuestionCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("accuracy")] float Accuracy,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("max_streak")] int MaxStreak,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("ended_at")] DateTime? EndedAt);

public sealed record AuditLog(
    [property: JsonPropertyName("log_id")] uint LogId,
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("entity_type")] string EntityType,
    [property: JsonPropertyName("entity_id")] uint? EntityId,
    [property: JsonPropertyName("ip_address")] string? IpAddress,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);
