namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record ProgressSummary(
    int CurrentStreakDays,
    int LongestStreakDays,
    float Accuracy7Days,
    int Correct7Days,
    int TotalAnswers7Days,
    int TotalWordsInProgress,
    int MasteredWords,
    int SessionsThisMonth);
