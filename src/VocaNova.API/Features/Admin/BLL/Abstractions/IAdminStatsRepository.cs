using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Models;

namespace VocaNova.API.Features.Admin.BLL.Abstractions;

public interface IAdminStatsRepository
{
    Task<AdminDashboardStatsModel> GetDashboardStatsAsync(
        DateTime todayUtc,
        DateTime tomorrowUtc,
        DateTime sevenDayStartUtc,
        CancellationToken cancellationToken = default);

    Task<AdminDemographicsModel> GetDemographicsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminWrongWordModel>> GetTopWrongWordsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminSessionAccuracyRow>> GetSessionAccuracyRowsAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminSessionCountRow>> GetSessionCountsByDayAsync(
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminMasteryCountRow>> GetMasteryDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminAuditLogModel>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
