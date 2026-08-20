using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services;
using VocaNova.API.Features.Knn.Contracts.Requests;
using VocaNova.API.Features.Knn.Mappings;

namespace VocaNova.API.Features.Knn.Controllers;

[ApiController]
[Authorize]
[Route("api/recommendations")]
public sealed class RecommendationsController : ControllerBase
{
    private readonly IKnnOnboardingService _knnOnboardingService;
    private readonly IKnnLearningService _knnLearningService;

    public RecommendationsController(
        IKnnOnboardingService knnOnboardingService,
        IKnnLearningService knnLearningService)
    {
        _knnOnboardingService = knnOnboardingService;
        _knnLearningService = knnLearningService;
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _knnOnboardingService.RecommendTopicsAsync(userId, limit, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Topic recommendations loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("words")]
    public async Task<IActionResult> GetWords([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _knnLearningService.GetWordRecommendationsAsync(userId, limit, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Word recommendations loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("personal-topics")]
    public async Task<IActionResult> GetPersonalTopics([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _knnOnboardingService.RecommendPersonalTopicsAsync(userId, limit, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Personal topic recommendations loaded successfully."))
            : ErrorResponse(result);
    }

    [AllowAnonymous]
    [HttpGet("learning-profile-options")]
    public async Task<IActionResult> GetLearningProfileOptions(CancellationToken cancellationToken)
    {
        var result = await _knnOnboardingService.GetLearningProfileOptionsAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(
                result.Value!.ToResponse(),
                "Learning profile options loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpPut("topics/selection")]
    public async Task<IActionResult> SelectTopics(
        [FromBody] SelectOnboardingTopicsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _knnOnboardingService.SelectTopicsAsync(
            userId,
            request.TopicIds ?? Array.Empty<uint>(),
            cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Onboarding topics saved successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("topics/{topicId:uint}/accept")]
    public async Task<IActionResult> AcceptTopic([FromRoute] uint topicId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _knnOnboardingService.AcceptTopicAsync(userId, topicId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Topic recommendation accepted successfully."))
            : ErrorResponse(result);
    }

    private bool TryGetCurrentUserId(out uint userId) =>
        uint.TryParse(User.FindFirst("user_id")?.Value, out userId);

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized, ApiResponseFormatter.Error("Unauthorized.", ["Unauthorized."]));

    private ObjectResult ErrorResponse<T>(KnnOperationResult<T> result)
    {
        var status = result.ErrorKind switch
        {
            KnnErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            KnnErrorKind.NotFound => StatusCodes.Status404NotFound,
            KnnErrorKind.Conflict => StatusCodes.Status409Conflict,
            KnnErrorKind.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, ApiResponseFormatter.Error(result.Error!, [result.Error!]));
    }
}
