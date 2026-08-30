using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.Stats;

// Mirror các DTO stats của VocaNova.API (F054 + phần bổ sung F056). Tên khớp chính xác snake_case của API.

public sealed record DashboardStats(
    [property: JsonPropertyName("total_users")] int TotalUsers,
    [property: JsonPropertyName("total_words")] int TotalWords,
    [property: JsonPropertyName("sessions_today")] int SessionsToday,
    [property: JsonPropertyName("avg_accuracy_7d")] double AvgAccuracy7d);

public sealed record SessionsTrend(
    [property: JsonPropertyName("days")] int Days,
    [property: JsonPropertyName("points")] IReadOnlyList<SessionTrendPoint> Points);

public sealed record SessionTrendPoint(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("session_count")] int SessionCount);

public sealed record MasteryDistribution(
    [property: JsonPropertyName("total_words_in_progress")] int TotalWordsInProgress,
    [property: JsonPropertyName("levels")] IReadOnlyList<MasteryLevelCount> Levels);

public sealed record MasteryLevelCount(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("word_count")] int WordCount);

// F062 — Statistics page.
public sealed record ActivityTrend(
    [property: JsonPropertyName("granularity")] string Granularity,
    [property: JsonPropertyName("points")] IReadOnlyList<ActivityTrendPoint> Points);

public sealed record ActivityTrendPoint(
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("sessions_count")] int SessionsCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record LearningStats(
    [property: JsonPropertyName("top_wrong_words")] IReadOnlyList<WrongWordStat> TopWrongWords,
    [property: JsonPropertyName("accuracy_trend")] IReadOnlyList<AccuracyTrendPoint> AccuracyTrend);

public sealed record WrongWordStat(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record AccuracyTrendPoint(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("accuracy")] double Accuracy);

public sealed record Demographics(
    [property: JsonPropertyName("age_ranges")] IReadOnlyList<DemographicGroup> AgeRanges,
    [property: JsonPropertyName("occupations")] IReadOnlyList<DemographicGroup> Occupations,
    [property: JsonPropertyName("education_levels")] IReadOnlyList<DemographicGroup> EducationLevels);

public sealed record DemographicGroup(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("user_count")] int UserCount);
