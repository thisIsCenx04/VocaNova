using VocaNova.Dashboard.Models.Api.Stats;

namespace VocaNova.Dashboard.Models.Dashboard;

public sealed class DashboardOverviewViewModel
{
    public AdminDashboardStatsDto? Stats { get; init; }

    public bool StatsLoaded => Stats is not null;

    public IReadOnlyList<AdminAccuracyTrendPointDto> Trend { get; init; } = Array.Empty<AdminAccuracyTrendPointDto>();

    public bool TrendLoaded { get; init; }
}
