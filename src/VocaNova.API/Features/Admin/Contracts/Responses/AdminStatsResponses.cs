using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.Contracts.Responses;

public sealed record AdminDashboardStatsResponse(
    [property: JsonPropertyName("total_users")] int TotalUsers,
    [property: JsonPropertyName("total_words")] int TotalWords,
    [property: JsonPropertyName("sessions_today")] int SessionsToday,
    [property: JsonPropertyName("avg_accuracy_7d")] double AvgAccuracy7d);

public sealed record AdminDemographicsResponse(
    [property: JsonPropertyName("age_ranges")] IReadOnlyCollection<AdminDemographicGroupResponse> AgeRanges,
    [property: JsonPropertyName("occupations")] IReadOnlyCollection<AdminDemographicGroupResponse> Occupations,
    [property: JsonPropertyName("education_levels")] IReadOnlyCollection<AdminDemographicGroupResponse> EducationLevels);

public sealed record AdminDemographicGroupResponse(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("user_count")] int UserCount);

public sealed record AdminLearningStatsResponse(
    [property: JsonPropertyName("top_wrong_words")] IReadOnlyCollection<AdminWrongWordResponse> TopWrongWords,
    [property: JsonPropertyName("accuracy_trend")] IReadOnlyCollection<AdminAccuracyTrendPointResponse> AccuracyTrend);

public sealed record AdminWrongWordResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record AdminAccuracyTrendPointResponse(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record AdminAuditLogResponse(
    [property: JsonPropertyName("log_id")] uint LogId,
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("entity_type")] string EntityType,
    [property: JsonPropertyName("entity_id")] uint? EntityId,
    [property: JsonPropertyName("payload_before")] string? PayloadBefore,
    [property: JsonPropertyName("payload_after")] string? PayloadAfter,
    [property: JsonPropertyName("ip_address")] string? IpAddress,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record AdminSessionsTrendResponse(
    [property: JsonPropertyName("days")] int Days,
    [property: JsonPropertyName("points")] IReadOnlyCollection<AdminSessionTrendPointResponse> Points);

public sealed record AdminSessionTrendPointResponse(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("session_count")] int SessionCount);

public sealed record AdminMasteryDistributionResponse(
    [property: JsonPropertyName("total_words_in_progress")] int TotalWordsInProgress,
    [property: JsonPropertyName("levels")] IReadOnlyCollection<AdminMasteryLevelResponse> Levels);

public sealed record AdminMasteryLevelResponse(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("word_count")] int WordCount);

public sealed record AdminActivityTrendResponse(
    [property: JsonPropertyName("granularity")] string Granularity,
    [property: JsonPropertyName("points")] IReadOnlyCollection<AdminActivityTrendPointResponse> Points);

public sealed record AdminActivityTrendPointResponse(
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("sessions_count")] int SessionsCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);
