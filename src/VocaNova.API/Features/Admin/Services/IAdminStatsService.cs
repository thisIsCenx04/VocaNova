using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Services;

public interface IAdminStatsService
{
    Task<Result<AdminDashboardStatsDto>> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminDemographicsDto>> GetDemographicsAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminLearningStatsDto>> GetLearningStatsAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminSessionsTrendDto>> GetSessionsTrendAsync(
        int days,
        CancellationToken cancellationToken = default);

    Task<Result<AdminMasteryDistributionDto>> GetMasteryDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AdminActivityTrendDto>> GetActivityTrendAsync(
        string granularity,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AdminAuditLogDto>>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
