using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Models;

namespace VocaNova.API.Features.Admin.BLL.Abstractions;

public interface IAdminStatsService
{
    Task<Result<AdminDashboardStatsModel>> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminDemographicsModel>> GetDemographicsAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminLearningStatsModel>> GetLearningStatsAsync(CancellationToken cancellationToken = default);

    Task<Result<AdminSessionsTrendModel>> GetSessionsTrendAsync(
        int days,
        CancellationToken cancellationToken = default);

    Task<Result<AdminMasteryDistributionModel>> GetMasteryDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AdminActivityTrendModel>> GetActivityTrendAsync(
        string granularity,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AdminAuditLogModel>>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
