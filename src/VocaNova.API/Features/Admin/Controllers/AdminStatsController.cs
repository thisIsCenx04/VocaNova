using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.API.Features.Admin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin")]
public sealed class AdminStatsController : ControllerBase
{
    private readonly IAdminStatsService _adminStatsService;

    public AdminStatsController(IAdminStatsService adminStatsService)
    {
        _adminStatsService = adminStatsService;
    }

    [HttpGet("stats/dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _adminStatsService.GetDashboardAsync(cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Dashboard stats loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpGet("stats/demographics")]
    public async Task<IActionResult> GetDemographics(CancellationToken cancellationToken)
    {
        var result = await _adminStatsService.GetDemographicsAsync(cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Demographics stats loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpGet("stats/learning")]
    public async Task<IActionResult> GetLearningStats(CancellationToken cancellationToken)
    {
        var result = await _adminStatsService.GetLearningStatsAsync(cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Learning stats loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] AdminAuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _adminStatsService.GetAuditLogsAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return Ok(ApiResponseFormatter.Paged(result.Value!, "Audit logs loaded successfully."));
    }
}
