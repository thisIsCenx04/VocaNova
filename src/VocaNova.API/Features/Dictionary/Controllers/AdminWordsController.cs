using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/words")]
public sealed class AdminWordsController : ControllerBase
{
    private const string EntityType = "words";

    private readonly IWordService _wordService;

    public AdminWordsController(IWordService wordService)
    {
        _wordService = wordService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(result.Value!.WordId);
        return this.CreatedResult(result.Value, "Word created successfully.");
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(
        [FromRoute] uint id,
        [FromBody] UpdateWordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value!, "Word updated successfully.");
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.SoftDeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "Word deleted successfully.");
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.RestoreAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "Word restored successfully.");
    }

    private void SetAuditEntity(uint wordId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = wordId;
    }
}
