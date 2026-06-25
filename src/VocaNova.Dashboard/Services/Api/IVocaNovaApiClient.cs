using VocaNova.Dashboard.Models.Api.Stats;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Client gọi các endpoint admin của VocaNova.API. Mọi request tự đính kèm Bearer token + refresh qua <see cref="BearerTokenHandler"/>.
/// </summary>
public interface IVocaNovaApiClient
{
    Task<DashboardStats?> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<SessionsTrend?> GetSessionsTrendAsync(int days, CancellationToken cancellationToken = default);

    Task<MasteryDistribution?> GetMasteryDistributionAsync(CancellationToken cancellationToken = default);
}
