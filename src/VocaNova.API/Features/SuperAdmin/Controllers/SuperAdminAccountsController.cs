using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.SuperAdmin.Contracts.Requests;
using VocaNova.API.Features.SuperAdmin.Contracts.Responses;
using VocaNova.API.Features.SuperAdmin.Mappings;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.SuperAdmin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
[Route("api/superadmin/admins")]
public sealed class SuperAdminAccountsController : ControllerBase
{
    private const string EntityType = "admin_accounts";
    private readonly ISuperAdminAccountService _service;

    public SuperAdminAccountsController(ISuperAdminAccountService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminAccountQueryRequest query, CancellationToken cancellationToken)
    {
        var result = await _service.GetAccountsAsync(query.ToModel(), cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        
        var mapped = new VocaNova.API.Common.Results.PagedResult<AdminAccountResponse>(
            result.Value!.Items.Select(x => x.ToResponse()).ToArray(),
            result.Value.Page,
            result.Value.Limit,
            result.Value.TotalItems);
            
        return Ok(ApiResponseFormatter.Paged(mapped, "Admin accounts loaded successfully."));
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAccountAsync(id, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!.ToResponse(), "Admin account loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.ToModel(), cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(result.Value!.AdminId);
        return this.CreatedResult(result.Value.ToResponse(), "Admin account created successfully.");
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(uint id, [FromBody] UpdateAdminAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request.ToModel(), cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(id);
        return this.OkResult(result.Value!.ToResponse(), "Admin account updated successfully.");
    }

    [HttpPatch("{id:uint}/lock")]
    public Task<IActionResult> Lock(uint id, CancellationToken cancellationToken) =>
        ChangeStatus(id, true, cancellationToken);

    [HttpPatch("{id:uint}/unlock")]
    public Task<IActionResult> Unlock(uint id, CancellationToken cancellationToken) =>
        ChangeStatus(id, false, cancellationToken);

    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(id);
        return this.OkResult(result.Value, "Admin account deleted successfully.");
    }

    private async Task<IActionResult> ChangeStatus(uint id, bool locked, CancellationToken cancellationToken)
    {
        var result = locked
            ? await _service.LockAsync(id, cancellationToken)
            : await _service.UnlockAsync(id, cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(id);
        return this.OkResult(result.Value, locked
            ? "Admin account locked successfully."
            : "Admin account unlocked successfully.");
    }

    private void SetAuditEntity(uint adminId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = adminId;
    }
}
