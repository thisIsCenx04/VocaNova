using VocaNova.Dashboard.Models.Api.Stats;

namespace VocaNova.Dashboard.Models.Statistics;

public sealed class StatisticsViewModel
{
    /// <summary>Top wrong words + accuracy trend (`GET /api/admin/stats/learning`).</summary>
    public AdminLearningStatsDto? Learning { get; init; }

    public bool LearningLoaded { get; init; }

    /// <summary>Phân bố nhân khẩu (`GET /api/admin/stats/demographics`).</summary>
    public AdminDemographicsDto? Demographics { get; init; }

    public bool DemographicsLoaded { get; init; }

    /// <summary>
    /// G7: API chưa nhận tham số granularity (daily/weekly/monthly) → dropdown render disabled,
    /// tạm cố định ở mức daily. Bật lên khi An thêm `?granularity=`.
    /// </summary>
    public bool GranularityAvailable { get; init; }
}
