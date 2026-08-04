using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizResultService : IQuizResultService
{
    private readonly IQuizResultRepository _quizResultRepository;

    public QuizResultService(IQuizResultRepository quizResultRepository)
    {
        _quizResultRepository = quizResultRepository;
    }

    public async Task<Result<QuizResultDto>> FinishSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default)
    {
        var sessionResult = await FindOwnedSessionAsync(userId, sessionId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return ToQuizResultFailure(sessionResult);
        }

        var session = sessionResult.Value!;
        if (session.Status != TestSessionStatus.InProgress)
        {
            return Result<QuizResultDto>.Conflict("Quiz session is not in progress.");
        }

        QuizSessionStatsCalculator.ApplyStats(session);
        session.Status = TestSessionStatus.Abandoned;
        session.EndedAt = DateTime.UtcNow;
        await _quizResultRepository.SaveChangesAsync(cancellationToken);

        return Result<QuizResultDto>.Ok(QuizSessionStatsCalculator.ToResultDto(session));
    }

    public async Task<Result<QuizResultDto>> GetResultAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default)
    {
        var sessionResult = await FindOwnedSessionAsync(userId, sessionId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return ToQuizResultFailure(sessionResult);
        }

        return Result<QuizResultDto>.Ok(QuizSessionStatsCalculator.ToResultDto(sessionResult.Value!));
    }

    private async Task<Result<TestSession>> FindOwnedSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken)
    {
        if (userId == 0)
        {
            return Result<TestSession>.Unauthorized("Unauthorized.");
        }

        if (sessionId == 0)
        {
            return Result<TestSession>.NotFound("Session not found.");
        }

        var session = await _quizResultRepository.FindSessionAsync(userId, sessionId, cancellationToken);
        if (session is null)
        {
            return Result<TestSession>.NotFound("Session not found.");
        }

        return Result<TestSession>.Ok(session);
    }

    private static Result<QuizResultDto> ToQuizResultFailure(Result<TestSession> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Result<QuizResultDto>.Unauthorized(result.Error!),
            StatusCodes.Status403Forbidden => Result<QuizResultDto>.Forbidden(result.Error!),
            StatusCodes.Status404NotFound => Result<QuizResultDto>.NotFound(result.Error!),
            StatusCodes.Status409Conflict => Result<QuizResultDto>.Conflict(result.Error!),
            _ => Result<QuizResultDto>.Fail(result.Error!),
        };
    }
}
