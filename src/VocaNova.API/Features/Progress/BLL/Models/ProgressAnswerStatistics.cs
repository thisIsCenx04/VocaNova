namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record ProgressAnswerStatistics(
    DateTime SessionStartedAt,
    bool IsCorrect);
