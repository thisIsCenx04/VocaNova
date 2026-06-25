using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Services;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.Admin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private const string EntityType = "users";

    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] AdminUserQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUsersAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return Ok(ApiResponseFormatter.Paged(result.Value!, "Users loaded successfully."));
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> GetUserDetail(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUserDetailAsync(id, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "User loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpGet("{id:uint}/test-history")]
    public async Task<IActionResult> GetTestHistory(
        [FromRoute] uint id,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminUserService.GetTestHistoryAsync(id, page, limit, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return Ok(ApiResponseFormatter.Paged(result.Value!, "User test history loaded successfully."));
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpPatch("{id:uint}/deactivate")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.DeactivateAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "User deactivated successfully.");
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.RestoreAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "User restored successfully.");
    }

    private void SetAuditEntity(uint userId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = userId;
    }
}
