using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.SuperAdmin.Contracts.Requests;
using VocaNova.API.Features.SuperAdmin.Contracts.Responses;
using VocaNova.API.Features.SuperAdmin.Mappings;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.API.Features.SuperAdmin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
[Route("api/superadmin/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleManagementService _service;
    public RolesController(IRoleManagementService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RoleQueryRequest query, CancellationToken cancellationToken)
    {
        var result = await _service.GetRolesAsync(query.ToModel(), cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);
        
        var mapped = new VocaNova.API.Common.Results.PagedResult<RoleResponse>(
            result.Value!.Items.Select(x => x.ToResponse()).ToArray(),
            result.Value.Page,
            result.Value.Limit,
            result.Value.TotalItems);
            
        return Ok(ApiResponseFormatter.Paged(mapped, "Roles loaded successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.ToModel(), cancellationToken);
        return result.IsSuccess ? this.CreatedResult(result.Value!.ToResponse(), "Role created successfully.") : this.ErrorResult(result);
    }

    [HttpPut("{roleId:uint}")]
    public async Task<IActionResult> Update(uint roleId, [FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(roleId, request.ToModel(), cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value!.ToResponse(), "Role updated successfully.") : this.ErrorResult(result);
    }

    [HttpDelete("{roleId:uint}")]
    public async Task<IActionResult> Delete(uint roleId, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(roleId, cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value, "Role deleted successfully.") : this.ErrorResult(result);
    }

    [HttpGet("{roleId:uint}/users")]
    public async Task<IActionResult> Users(uint roleId, CancellationToken cancellationToken)
    {
        var result = await _service.GetUsersAsync(roleId, cancellationToken);
        return result.IsSuccess 
            ? this.OkResult(result.Value!.Select(x => x.ToResponse()).ToArray(), "Role users loaded successfully.") 
            : this.ErrorResult(result);
    }

    [HttpPost("{roleId:uint}/users/{userId:uint}")]
    public async Task<IActionResult> Assign(uint roleId, uint userId, CancellationToken cancellationToken)
    {
        var result = await _service.AssignRoleAsync(roleId, userId, cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value, "Role assigned successfully.") : this.ErrorResult(result);
    }

    [HttpDelete("{roleId:uint}/users/{userId:uint}")]
    public async Task<IActionResult> Remove(uint roleId, uint userId, CancellationToken cancellationToken)
    {
        var result = await _service.RemoveRoleAsync(roleId, userId, cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value, "Role removed successfully.") : this.ErrorResult(result);
    }
}
