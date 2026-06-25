using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Stats;

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
