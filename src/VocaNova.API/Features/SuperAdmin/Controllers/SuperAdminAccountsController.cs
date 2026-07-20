using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Features.SuperAdmin.Services;
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
    public async Task<IActionResult> List([FromQuery] AdminAccountQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.GetAccountsAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Paged(result.Value!, "Admin accounts loaded successfully."))
            : this.ErrorResult(result);
    }

    [HttpGet("{id:uint}")]
    public async Task<IActionResult> Detail(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAccountAsync(id, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Admin account loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(result.Value!.AdminId);
        return this.CreatedResult(result.Value, "Admin account created successfully.");
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(uint id, [FromBody] UpdateAdminAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        SetAuditEntity(id);
        return this.OkResult(result.Value!, "Admin account updated successfully.");
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
