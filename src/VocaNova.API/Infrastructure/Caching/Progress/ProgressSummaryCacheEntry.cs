using System.Text.Json.Serialization;
using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Infrastructure.Caching.Progress;

internal sealed record ProgressSummaryCacheEntry(
    [property: JsonPropertyName("current_streak_days")] int CurrentStreakDays,
    [property: JsonPropertyName("longest_streak_days")] int LongestStreakDays,
    [property: JsonPropertyName("accuracy_7d")] float Accuracy7Days,
    [property: JsonPropertyName("correct_7d")] int Correct7Days,
    [property: JsonPropertyName("total_answers_7d")] int TotalAnswers7Days,
    [property: JsonPropertyName("total_words_in_progress")] int TotalWordsInProgress,
    [property: JsonPropertyName("mastered_words")] int MasteredWords,
    [property: JsonPropertyName("sessions_this_month")] int SessionsThisMonth)
{
    public static ProgressSummaryCacheEntry FromBusinessModel(ProgressSummary summary) =>
        new(
            summary.CurrentStreakDays,
            summary.LongestStreakDays,
            summary.Accuracy7Days,
            summary.Correct7Days,
            summary.TotalAnswers7Days,
            summary.TotalWordsInProgress,
            summary.MasteredWords,
            summary.SessionsThisMonth);

    public ProgressSummary ToBusinessModel() =>
        new(
            CurrentStreakDays,
            LongestStreakDays,
            Accuracy7Days,
            Correct7Days,
            TotalAnswers7Days,
            TotalWordsInProgress,
            MasteredWords,
            SessionsThisMonth);
}
