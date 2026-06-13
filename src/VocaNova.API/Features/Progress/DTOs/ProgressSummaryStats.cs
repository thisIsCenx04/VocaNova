namespace VocaNova.API.Features.Progress.DTOs;

public sealed record ProgressSummaryStats(
    IReadOnlyCollection<DateOnly> SessionDates,
    int Correct7Days,
    int TotalAnswers7Days,
    int TotalWordsInProgress,
    int MasteredWords,
    int SessionsThisMonth);
