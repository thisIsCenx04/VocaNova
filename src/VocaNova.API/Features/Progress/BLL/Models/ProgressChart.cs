namespace VocaNova.API.Features.Progress.BLL.Models;

public sealed record ProgressChart(
    string Granularity,
    IReadOnlyCollection<ProgressChartPoint> Points);

public sealed record ProgressChartPoint(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeriodLabel,
    int SessionsCount,
    int CorrectCount,
    int TotalAnswers,
    float Accuracy);
