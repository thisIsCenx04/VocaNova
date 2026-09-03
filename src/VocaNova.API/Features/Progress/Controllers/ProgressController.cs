using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Features.Progress.BLL.Services;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Progress.Contracts.Requests;
using VocaNova.API.Features.Progress.Mappings;
using VocaNova.API.Features.Progress.BLL.Services.IServices;

namespace VocaNova.API.Features.Progress.Controllers;

[ApiController]
[Authorize]
[Route("api/progress")]
public sealed class ProgressController : ControllerBase
{
    private readonly IProgressSummaryService _progressSummaryService;
    private readonly IProgressAnalyticsService _progressAnalyticsService;

    public ProgressController(
        IProgressSummaryService progressSummaryService,
        IProgressAnalyticsService progressAnalyticsService)
    {
        _progressSummaryService = progressSummaryService;
        _progressAnalyticsService = progressAnalyticsService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _progressSummaryService.GetSummaryAsync(userId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Progress summary loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] ProgressChartRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _progressAnalyticsService.GetChartAsync(
            userId,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Progress chart loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("mastery-breakdown")]
    public async Task<IActionResult> GetMasteryBreakdown(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _progressAnalyticsService.GetMasteryBreakdownAsync(
            userId,
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Mastery breakdown loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("weakest-words")]
    public async Task<IActionResult> GetWeakestWords(
        [FromQuery] WeakestWordsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _progressAnalyticsService.GetWeakestWordsAsync(
            userId,
            request.ToBusinessQuery(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Weakest words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("words/{wordId:uint}")]
    public async Task<IActionResult> GetWordProgress(
        [FromRoute] uint wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return UnauthorizedResponse();
        }

        var result = await _progressAnalyticsService.GetWordProgressAsync(
            userId,
            wordId,
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Word progress loaded successfully."))
            : ErrorResponse(result);
    }

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(
            StatusCodes.Status401Unauthorized,
            ApiResponseFormatter.Error("Unauthorized.", new[] { "Unauthorized." }));

    private ObjectResult ErrorResponse<T>(ProgressResult<T> result)
    {
        var statusCode = result.ErrorKind switch
        {
            ProgressErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ProgressErrorKind.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(
            statusCode,
            ApiResponseFormatter.Error(result.Error!, new[] { result.Error! }));
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
