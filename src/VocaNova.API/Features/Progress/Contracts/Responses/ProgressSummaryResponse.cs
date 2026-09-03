using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.Contracts.Responses;

public sealed record ProgressSummaryResponse(
    [property: JsonPropertyName("current_streak_days")] int CurrentStreakDays,
    [property: JsonPropertyName("longest_streak_days")] int LongestStreakDays,
    [property: JsonPropertyName("accuracy_7d")] float Accuracy7Days,
    [property: JsonPropertyName("correct_7d")] int Correct7Days,
    [property: JsonPropertyName("total_answers_7d")] int TotalAnswers7Days,
    [property: JsonPropertyName("total_words_in_progress")] int TotalWordsInProgress,
    [property: JsonPropertyName("mastered_words")] int MasteredWords,
    [property: JsonPropertyName("sessions_this_month")] int SessionsThisMonth);
