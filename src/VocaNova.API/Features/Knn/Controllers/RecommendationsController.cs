using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Services;

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
    public async Task<IActionResult> GetTopics(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<IReadOnlyCollection<TopicRecommendationDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _knnOnboardingService.RecommendTopicsAsync(userId, limit, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Topic recommendations loaded successfully.");
    }

    [HttpGet("words")]
    public async Task<IActionResult> GetWords(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<IReadOnlyCollection<WordRecommendationDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _knnLearningService.GetWordRecommendationsAsync(userId, limit, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Word recommendations loaded successfully.");
    }

    /// <summary>
    /// Lookup catalog for the sign-up form and the onboarding questions. Anonymous because the
    /// sign-up form has to render it before an account exists.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("learning-profile-options")]
    public async Task<IActionResult> GetLearningProfileOptions(CancellationToken cancellationToken)
    {
        var result = await _knnOnboardingService.GetLearningProfileOptionsAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Learning profile options loaded successfully.");
    }

    [HttpPut("topics/selection")]
    public async Task<IActionResult> SelectTopics(
        [FromBody] SelectOnboardingTopicsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<int>.Unauthorized("Unauthorized."));
        }

        var result = await _knnOnboardingService.SelectTopicsAsync(
            userId,
            request.TopicIds ?? Array.Empty<uint>(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Onboarding topics saved successfully.");
    }

    [HttpPost("topics/{topicId:uint}/accept")]
    public async Task<IActionResult> AcceptTopic(
        [FromRoute] uint topicId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _knnOnboardingService.AcceptTopicAsync(userId, topicId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Topic recommendation accepted successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
