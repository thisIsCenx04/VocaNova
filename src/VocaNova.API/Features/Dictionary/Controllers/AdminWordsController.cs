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
[Route("api/admin/words")]
public sealed class AdminWordsController : ControllerBase
{
    private readonly IWordAdminService _service;
    public AdminWordsController(IWordAdminService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminWordQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(request.ToBusinessQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Paged(result.Value!.ToResponse(), "Words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWordRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", result.Value!.WordId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponseFormatter.Created(result.Value.ToResponse(), "Word created successfully."));
    }

    [HttpPut("{id:uint}")]
    public async Task<IActionResult> Update(uint id, [FromBody] UpdateWordRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Word updated successfully."));
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportWordsRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File?.OpenReadStream();
        var result = await _service.ImportCsvAsync(request.File.ToUploadedContent(stream), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "words";
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Words imported successfully."));
    }

    [HttpPost("{id:uint}/audio")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAudio(uint id, [FromForm] UploadWordAudioRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File?.OpenReadStream();
        var result = await _service.UploadAudioAsync(id, request.Accent,
            request.File.ToUploadedContent(stream), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_audio_assets", result.Value!.AudioId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponseFormatter.Created(result.Value.ToResponse(), "Audio uploaded successfully."));
    }

    [HttpDelete("{id:uint}/audio/{audioId:uint}")]
    public async Task<IActionResult> SoftDeleteAudio(uint id, uint audioId, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAudioAsync(id, audioId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_audio_assets", audioId);
        return Ok(ApiResponseFormatter.Success(result.Value, "Audio deleted successfully."));
    }

    [HttpPost("{id:uint}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(uint id, [FromForm] UploadWordImageRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File?.OpenReadStream();
        var result = await _service.UploadImageAsync(id, request.File.ToUploadedContent(stream), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Word image uploaded successfully."));
    }

    [HttpPut("{id:uint}/image")]
    public async Task<IActionResult> UpdateImageUrl(uint id, [FromBody] UpdateWordImageRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateImageUrlAsync(id, request.ImageUrl, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", id);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Word image updated successfully."));
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpDelete("{id:uint}")]
    public async Task<IActionResult> SoftDelete(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", id);
        return Ok(ApiResponseFormatter.Success(result.Value, "Word deleted successfully."));
    }

    [Authorize(Policy = JwtAuthenticationExtensions.SuperAdminPolicy)]
    [HttpPatch("{id:uint}/restore")]
    public async Task<IActionResult> Restore(uint id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("words", id);
        return Ok(ApiResponseFormatter.Success(result.Value, "Word restored successfully."));
    }

    [HttpPost("{id:uint}/senses")]
    public async Task<IActionResult> CreateSense(uint id, [FromBody] CreateSenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateSenseAsync(id, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_senses", result.Value!.SenseId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponseFormatter.Created(result.Value.ToResponse(), "Sense created successfully."));
    }

    [HttpPut("{id:uint}/senses/{senseId:uint}")]
    public async Task<IActionResult> UpdateSense(uint id, uint senseId, [FromBody] UpdateSenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateSenseAsync(id, senseId, request.ToBusinessCommand(), cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_senses", senseId);
        return Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Sense updated successfully."));
    }

    [HttpDelete("{id:uint}/senses/{senseId:uint}")]
    public async Task<IActionResult> SoftDeleteSense(uint id, uint senseId, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteSenseAsync(id, senseId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_senses", senseId);
        return Ok(ApiResponseFormatter.Success(result.Value, "Sense deleted successfully."));
    }

    [HttpPatch("{id:uint}/senses/{senseId:uint}/restore")]
    public async Task<IActionResult> RestoreSense(uint id, uint senseId, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreSenseAsync(id, senseId, cancellationToken);
        if (!result.IsSuccess) return ErrorResponse(result);
        SetAuditEntity("word_senses", senseId);
        return Ok(ApiResponseFormatter.Success(result.Value, "Sense restored successfully."));
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

    private void SetAuditEntity(string type, uint id)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = type;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = id;
    }
}
