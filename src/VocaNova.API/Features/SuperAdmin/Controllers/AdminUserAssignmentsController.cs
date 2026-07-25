using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Features.SuperAdmin.Services;
using VocaNova.API.Infrastructure.Authentication;

namespace VocaNova.API.Features.SuperAdmin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
[Route("api/superadmin/admin-user-assignments")]
public sealed class AdminUserAssignmentsController : ControllerBase
{
    private readonly AdminUserAssignmentService _service;
    public AdminUserAssignmentsController(AdminUserAssignmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value!, "Assignments loaded successfully.") : this.ErrorResult(result);
    }

    [HttpPut("{adminId:uint}")]
    public async Task<IActionResult> Replace(
        uint adminId,
        [FromBody] SaveAdminUserAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReplaceAsync(adminId, request.UserIds, cancellationToken);
        return result.IsSuccess ? this.OkResult(result.Value, "Assignments saved successfully.") : this.ErrorResult(result);
    }
}
