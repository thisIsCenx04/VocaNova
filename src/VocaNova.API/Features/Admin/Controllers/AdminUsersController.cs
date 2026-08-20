using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Admin.Contracts.Requests;
using VocaNova.API.Features.Admin.Contracts.Responses;
using VocaNova.API.Features.Admin.Mappings;
using VocaNova.API.Features.Admin.BLL.Abstractions;
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
        [FromQuery] AdminUserQueryRequest query,
        CancellationToken cancellationToken)
    {
        var bllQuery = new VocaNova.API.Features.Admin.BLL.Models.AdminUserQuery(query.Page, query.Limit, query.Status, query.Search, query.IncludeDeleted, query.Role, query.SortBy, query.SortDirection);
        var result = await _adminUserService.GetUsersAsync(bllQuery, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        var mappedPagedResult = new VocaNova.API.Common.Results.PagedResult<AdminUserSummaryResponse>(
            result.Value!.Items.Select(x => x.ToResponse()).ToArray(),
            result.Value.Page,
            result.Value.Limit,
            result.Value.TotalItems);

        return Ok(ApiResponseFormatter.Paged(mappedPagedResult, "Users loaded successfully."));
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> GetUserDetail(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUserDetailAsync(id, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!.ToResponse(), "User loaded successfully.")
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

        var mappedPagedResult = new VocaNova.API.Common.Results.PagedResult<AdminUserTestSessionResponse>(
            result.Value!.Items.Select(x => x.ToResponse()).ToArray(),
            result.Value.Page,
            result.Value.Limit,
            result.Value.TotalItems);

        return Ok(ApiResponseFormatter.Paged(mappedPagedResult, "User test history loaded successfully."));
    }

    [HttpGet("{id:uint}/topics")]
    public async Task<IActionResult> GetTopics(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUserTopicsAsync(id, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!.ToResponse(), "User topics loaded successfully.")
            : this.ErrorResult(result);
    }

    // Admin được khóa user thường; chỉ super_admin mới khóa được admin (kiểm tra trong service).
    [HttpPatch("{id:uint}/deactivate")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.DeactivateAsync(id, User.FindFirst("role")?.Value ?? string.Empty, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "User deactivated successfully.");
    }

    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.RestoreAsync(id, User.FindFirst("role")?.Value ?? string.Empty, cancellationToken);
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
