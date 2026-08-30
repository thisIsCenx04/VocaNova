using VocaNova.Dashboard.Data.Dtos.Stats;

namespace VocaNova.Dashboard.Models.Dashboard;

public sealed class DashboardOverviewViewModel
{
    public DashboardStats? Stats { get; init; }

    public IReadOnlyList<DifficultWordRow> DifficultWords { get; init; } = Array.Empty<DifficultWordRow>();

    public MasteryDistribution? Mastery { get; init; }

    public ActivityTrend? Activity { get; init; }
}

// Một dòng "Most Difficult Words": rank + từ + số lượt + tỉ lệ sai + mức độ.
public sealed record DifficultWordRow(
    int Rank,
    string Word,
    int Attempts,
    int FailureRate,
    string Severity,
    string StatusLabel);
