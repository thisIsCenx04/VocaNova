using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Mappings;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/topics")]
public sealed class AdminTopicsController : ControllerBase
{
    private readonly ITopicAdminService _service;
    public AdminTopicsController(ITopicAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminTopicQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(request.ToBusinessQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Topics loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity(result.Value!.TopicId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponseFormatter.Created(result.Value.ToResponse(), "Topic created successfully."));
    }

    [HttpPost("{id:uint}/words")]
    public async Task<IActionResult> AddWords(uint id, [FromBody] AddTopicWordsRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddWordsAsync(id, request.WordIds, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity(id);
        return Ok(ApiResponseFormatter.Success(new { added = result.Value }, "Vocabulary added to topic successfully."));
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(uint id, [FromBody] UpdateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity(id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Topic updated successfully."));
    }

    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity(id);
        return Ok(ApiResponseFormatter.Success(result.Value, "Topic deleted successfully."));
    }

    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity(id);
        return Ok(ApiResponseFormatter.Success(result.Value, "Topic restored successfully."));
    }

    private ObjectResult ErrorResponse<T>(DictionaryResult<T> result)
    {
        var status = result.ErrorKind switch
        {
            DictionaryErrorKind.NotFound => StatusCodes.Status404NotFound,
            DictionaryErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, ApiResponseFormatter.Error(result.Error!, [result.Error!]));
    }

    private void SetAuditEntity(uint id)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "topics";
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = id;
    }
}
