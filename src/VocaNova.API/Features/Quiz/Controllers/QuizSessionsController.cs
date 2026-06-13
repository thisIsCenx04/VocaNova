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

    public QuizSessionsController(IQuizSessionService quizSessionService)
    {
        _quizSessionService = quizSessionService;
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

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
