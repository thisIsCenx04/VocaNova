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

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(
        [FromForm] ImportWordsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.ImportCsvAsync(request.File, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        return this.OkResult(result.Value!, "Words imported successfully.");
    }

    [HttpPost("{id:uint}/audio")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAudio(
        [FromRoute] uint id,
        [FromForm] UploadWordAudioRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.UploadAudioAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAudioAuditEntity(result.Value!.AudioId);
        return this.CreatedResult(result.Value, "Audio uploaded successfully.");
    }

    [HttpDelete("{id:uint}/audio/{audioId:uint}")]
    public async Task<IActionResult> SoftDeleteAudio(
        [FromRoute] uint id,
        [FromRoute] uint audioId,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.SoftDeleteAudioAsync(id, audioId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAudioAuditEntity(audioId);
        return this.OkResult(result.Value, "Audio deleted successfully.");
    }

    [HttpPost("{id:uint}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
        [FromRoute] uint id,
        [FromForm] UploadWordImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.UploadImageAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value!, "Word image uploaded successfully.");
    }

    [HttpPut("{id:uint}/image")]
    public async Task<IActionResult> UpdateImageUrl(
        [FromRoute] uint id,
        [FromBody] UpdateWordImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.UpdateImageUrlAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetAuditEntity(id);
        return this.OkResult(result.Value!, "Word image updated successfully.");
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

    [HttpPost("{id:uint}/senses")]
    public async Task<IActionResult> CreateSense(
        [FromRoute] uint id,
        [FromBody] CreateSenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.CreateSenseAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetSenseAuditEntity(result.Value!.SenseId);
        return this.CreatedResult(result.Value, "Sense created successfully.");
    }

    [HttpPut("{id:uint}/senses/{senseId:uint}")]
    public async Task<IActionResult> UpdateSense(
        [FromRoute] uint id,
        [FromRoute] uint senseId,
        [FromBody] UpdateSenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.UpdateSenseAsync(id, senseId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetSenseAuditEntity(senseId);
        return this.OkResult(result.Value!, "Sense updated successfully.");
    }

    [HttpDelete("{id:uint}/senses/{senseId:uint}")]
    public async Task<IActionResult> SoftDeleteSense(
        [FromRoute] uint id,
        [FromRoute] uint senseId,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.SoftDeleteSenseAsync(id, senseId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetSenseAuditEntity(senseId);
        return this.OkResult(result.Value, "Sense deleted successfully.");
    }

    [HttpPatch("{id:uint}/senses/{senseId:uint}/restore")]
    public async Task<IActionResult> RestoreSense(
        [FromRoute] uint id,
        [FromRoute] uint senseId,
        CancellationToken cancellationToken)
    {
        var result = await _wordService.RestoreSenseAsync(id, senseId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        SetSenseAuditEntity(senseId);
        return this.OkResult(result.Value, "Sense restored successfully.");
    }

    private void SetAuditEntity(uint wordId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = EntityType;
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = wordId;
    }

    private void SetSenseAuditEntity(uint senseId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "word_senses";
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = senseId;
    }

    private void SetAudioAuditEntity(uint audioId)
    {
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "word_audio_assets";
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = audioId;
    }
}
