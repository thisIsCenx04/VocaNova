using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.DTOs;

public sealed record AdminDashboardStatsDto(
    [property: JsonPropertyName("total_users")] int TotalUsers,
    [property: JsonPropertyName("total_words")] int TotalWords,
    [property: JsonPropertyName("sessions_today")] int SessionsToday,
    [property: JsonPropertyName("avg_accuracy_7d")] double AvgAccuracy7d);

public sealed record AdminDemographicsDto(
    [property: JsonPropertyName("age_ranges")] IReadOnlyCollection<AdminDemographicGroupDto> AgeRanges,
    [property: JsonPropertyName("occupations")] IReadOnlyCollection<AdminDemographicGroupDto> Occupations,
    [property: JsonPropertyName("education_levels")] IReadOnlyCollection<AdminDemographicGroupDto> EducationLevels);

public sealed record AdminDemographicGroupDto(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("user_count")] int UserCount);

public sealed record AdminLearningStatsDto(
    [property: JsonPropertyName("top_wrong_words")] IReadOnlyCollection<AdminWrongWordDto> TopWrongWords,
    [property: JsonPropertyName("accuracy_trend")] IReadOnlyCollection<AdminAccuracyTrendPointDto> AccuracyTrend);

public sealed record AdminWrongWordDto(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record AdminAccuracyTrendPointDto(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record AdminAuditLogQuery(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("user_id")] uint? UserId = null,
    [property: JsonPropertyName("entity")] string? Entity = null);

public sealed record AdminAuditLogDto(
    [property: JsonPropertyName("log_id")] uint LogId,
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("entity_type")] string EntityType,
    [property: JsonPropertyName("entity_id")] uint? EntityId,
    [property: JsonPropertyName("payload_before")] string? PayloadBefore,
    [property: JsonPropertyName("payload_after")] string? PayloadAfter,
    [property: JsonPropertyName("ip_address")] string? IpAddress,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed record AdminSessionAccuracyRow(
    DateOnly Date,
    int CorrectCount,
    int WrongCount);
