using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Repositories;

public interface IAdminStatsRepository
{
    Task<AdminDashboardStatsDto> GetDashboardStatsAsync(
        DateTime todayUtc,
        DateTime tomorrowUtc,
        DateTime sevenDayStartUtc,
        CancellationToken cancellationToken = default);

    Task<AdminDemographicsDto> GetDemographicsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminWrongWordDto>> GetTopWrongWordsAsync(
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

    Task<PagedResult<AdminAuditLogDto>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
