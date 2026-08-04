using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Services;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.Admin.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/topics")]
public sealed class AdminTopicsController : ControllerBase
{
    private const string EntityType = "topics";

    private readonly ITopicService _topicService;

    public AdminTopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AdminTopicQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.GetAdminTopicsAsync(query, cancellationToken);
        return result.IsSuccess
            ? this.OkResult(result.Value!, "Topics loaded successfully.")
            : this.ErrorResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTopicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(result.Value!.TopicId);
        return this.CreatedResult(result.Value, "Topic created successfully.");
    }

    [HttpPost("{id:uint}/words")]
    public async Task<IActionResult> AddWords(
        [FromRoute] uint id,
        [FromBody] AddTopicWordsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.AddWordsAsync(id, request, cancellationToken);
        if (!result.IsSuccess) return this.ErrorResult(result);

        SetAuditEntity(id);
        return this.OkResult(new { added = result.Value }, "Vocabulary added to topic successfully.");
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(
        [FromRoute] uint id,
        [FromBody] UpdateTopicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value!, "Topic updated successfully.");
    }

    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.SoftDeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "Topic deleted successfully.");
    }

    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] uint id,
        CancellationToken cancellationToken)
    {
        var result = await _topicService.RestoreAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value, "Topic restored successfully.");
    }

    private void SetAuditEntity(uint topicId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = topicId;
    }
}
