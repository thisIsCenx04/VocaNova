using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Mappings;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Features.Dictionary.Controllers;

[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicy)]
[Route("api/admin/words/{id:uint}/video")]
public sealed class AdminWordVideosController(IWordVideoService service) : ControllerBase
{
    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(22 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 22 * 1024 * 1024)]
    public async Task<IActionResult> Save(uint id, [FromForm] UploadWordVideoRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File?.OpenReadStream();
        var result = await service.SaveAsync(id, request.File.ToUploadedContent(stream), cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, ApiResponseFormatter.Error(result.Error!));
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "word_video_assets";
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = result.Value!.VideoId.ToString();
        return Ok(ApiResponseFormatter.Success(result.Value.ToResponse(), "Video saved successfully."));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, ApiResponseFormatter.Error(result.Error!));
        HttpContext.Items[AuditLogHttpContextKeys.EntityType] = "words";
        HttpContext.Items[AuditLogHttpContextKeys.EntityId] = id.ToString();
        return Ok(ApiResponseFormatter.Success(true, "Video deleted successfully."));
    }
}
