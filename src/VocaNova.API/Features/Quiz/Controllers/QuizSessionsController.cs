using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.BLL.Services;
using VocaNova.API.Features.Quiz.BLL.Services.IServices;
using VocaNova.API.Features.Quiz.Contracts.Requests;
using VocaNova.API.Features.Quiz.Mappings;

namespace VocaNova.API.Features.Quiz.Controllers;

[ApiController]
[Authorize]
[Route("api/quiz/sessions")]
public sealed class QuizSessionsController : ControllerBase
{
    private readonly IQuizSessionService _sessionService;
    private readonly IQuizSubmissionService _submissionService;
    private readonly IQuizResultService _resultService;
    private readonly IQuizHistoryService _historyService;

    public QuizSessionsController(IQuizSessionService sessionService,
        IQuizSubmissionService submissionService, IQuizResultService resultService,
        IQuizHistoryService historyService)
    {
        _sessionService = sessionService;
        _submissionService = submissionService;
        _resultService = resultService;
        _historyService = historyService;
    }

    [HttpGet("/api/quiz/history")]
    public async Task<IActionResult> GetHistory([FromQuery] QuizHistoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _historyService.GetHistoryAsync(userId, request.ToBusinessQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Quiz history loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("/api/quiz/wrong-words")]
    public async Task<IActionResult> GetWrongWords([FromQuery] WrongWordsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _historyService.GetWrongWordsAsync(userId, request.ToBusinessQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Wrong words loaded successfully."))
            : ErrorResponse(result);
    }

    [HttpDelete("/api/quiz/wrong-words/{wordId:uint}")]
    public async Task<IActionResult> ClearWrongWord(uint wordId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _historyService.ClearWrongWordAsync(userId, wordId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Wrong word removed successfully."))
            : ErrorResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _sessionService.CreateSessionAsync(userId, request.ToBusinessCommand(), cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created,
                ApiResponseFormatter.Created(result.Value!.ToResponse(), "Quiz session created successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("{id:uint}/answer")]
    public async Task<IActionResult> SubmitAnswer(uint id, [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _submissionService.SubmitAnswerAsync(userId, id,
            request.ToBusinessCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Answer submitted successfully."))
            : ErrorResponse(result);
    }

    [HttpPost("{id:uint}/finish")]
    public async Task<IActionResult> Finish(uint id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _resultService.FinishSessionAsync(userId, id, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Quiz session finished successfully."))
            : ErrorResponse(result);
    }

    [HttpGet("{id:uint}/result")]
    public async Task<IActionResult> GetResult(uint id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return UnauthorizedResponse();
        var result = await _resultService.GetResultAsync(userId, id, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Quiz result loaded successfully."))
            : ErrorResponse(result);
    }

    private bool TryGetCurrentUserId(out uint userId) =>
        uint.TryParse(User.FindFirst("user_id")?.Value, out userId);

    private ObjectResult UnauthorizedResponse() => StatusCode(StatusCodes.Status401Unauthorized,
        ApiResponseFormatter.Error("Unauthorized.", ["Unauthorized."]));

    private ObjectResult ErrorResponse<T>(QuizOperationResult<T> result)
    {
        var status = result.ErrorKind switch
        {
            QuizErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            QuizErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            QuizErrorKind.NotFound => StatusCodes.Status404NotFound,
            QuizErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return StatusCode(status, ApiResponseFormatter.Error(result.Error!, [result.Error!]));
    }
}
