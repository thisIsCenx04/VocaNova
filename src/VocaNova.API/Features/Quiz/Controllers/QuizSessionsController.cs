using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Services;

namespace VocaNova.API.Features.Quiz.Controllers;

[ApiController]
[Authorize]
[Route("api/quiz/sessions")]
public sealed class QuizSessionsController : ControllerBase
{
    private readonly IQuizSessionService _quizSessionService;
    private readonly IQuizSubmitService _quizSubmitService;

    public QuizSessionsController(
        IQuizSessionService quizSessionService,
        IQuizSubmitService quizSubmitService)
    {
        _quizSessionService = quizSessionService;
        _quizSubmitService = quizSubmitService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<CreateSessionResponse>.Unauthorized("Unauthorized."));
        }

        var result = await _quizSessionService.CreateSessionAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.CreatedResult(result.Value!, "Quiz session created successfully.");
    }

    [HttpPost("{id:uint}/answer")]
    public async Task<IActionResult> SubmitAnswer(
        [FromRoute] uint id,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<AnswerResultDto>.Unauthorized("Unauthorized."));
        }

        var result = await _quizSubmitService.SubmitAnswerAsync(userId, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Answer submitted successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
