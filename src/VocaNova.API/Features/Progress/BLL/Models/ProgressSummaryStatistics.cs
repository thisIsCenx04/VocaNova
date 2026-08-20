namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record ProgressSummaryStatistics(
    IReadOnlyCollection<DateOnly> SessionDates,
    int Correct7Days,
    int TotalAnswers7Days,
    int TotalWordsInProgress,
    int MasteredWords,
    int SessionsThisMonth);
