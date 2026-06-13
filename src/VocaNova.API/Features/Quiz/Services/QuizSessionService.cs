using Microsoft.AspNetCore.Http;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizSessionService : IQuizSessionService
{
    private const string NotEnoughWordsMessage = "Không đủ từ để tạo bài kiểm tra";

    private static readonly IReadOnlySet<int> QuestionTypes = new HashSet<int>
    {
        int.Parse(QuestionType.WordToMeaning),
        int.Parse(QuestionType.MeaningToWord),
        int.Parse(QuestionType.Description),
    };

    private readonly IQuizSessionBuilder _quizSessionBuilder;
    private readonly IQuizQuestionBuilder _quizQuestionBuilder;
    private readonly IQuizSessionRepository _quizSessionRepository;
    private readonly IProgressSummaryCache? _progressSummaryCache;

    public QuizSessionService(
        IQuizSessionBuilder quizSessionBuilder,
        IQuizQuestionBuilder quizQuestionBuilder,
        IQuizSessionRepository quizSessionRepository,
        IProgressSummaryCache? progressSummaryCache = null)
    {
        _quizSessionBuilder = quizSessionBuilder;
        _quizQuestionBuilder = quizQuestionBuilder;
        _quizSessionRepository = quizSessionRepository;
        _progressSummaryCache = progressSummaryCache;
    }

    public async Task<Result<CreateSessionResponse>> CreateSessionAsync(
        uint userId,
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<CreateSessionResponse>.Unauthorized("Unauthorized.");
        }

        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
        {
            return Result<CreateSessionResponse>.Fail(validationResult.Error!);
        }

        var poolResult = await _quizSessionBuilder.BuildPoolAsync(
            userId,
            new BuildQuizPoolRequest(
                request.ScopeType!,
                request.ScopeDateFrom,
                request.ScopeDateTo,
                request.TopicIds,
                request.WordOrder!,
                request.WordLimit,
                request.AnswerMethod!),
            cancellationToken);
        if (!poolResult.IsSuccess)
        {
            return ToCreateSessionFailure(poolResult);
        }

        var pool = poolResult.Value!;
        if (pool.Count == 0)
        {
            return Result<CreateSessionResponse>.Fail(NotEnoughWordsMessage);
        }

        var firstWord = pool.First();
        var questionResult = await _quizQuestionBuilder.BuildQuestionAsync(
            firstWord.WordId,
            request.QuestionType,
            cancellationToken);
        if (!questionResult.IsSuccess)
        {
            return ToCreateSessionFailure(questionResult);
        }

        var session = await _quizSessionRepository.CreateAsync(
            userId,
            request,
            pool.Count,
            cancellationToken);
        if (_progressSummaryCache is not null)
        {
            await _progressSummaryCache.RemoveAsync(userId, cancellationToken);
        }

        return Result<CreateSessionResponse>.Ok(new CreateSessionResponse(
            session,
            questionResult.Value!));
    }

    private static Result<bool> ValidateRequest(CreateSessionRequest request)
    {
        if (request.Mode is null || !TestMode.All.Contains(request.Mode))
        {
            return Result<bool>.Fail("Mode is invalid.");
        }

        if (!QuestionTypes.Contains(request.QuestionType))
        {
            return Result<bool>.Fail("Question type is invalid.");
        }

        if (request.ScopeType is null || !ScopeType.Values.Contains(request.ScopeType))
        {
            return Result<bool>.Fail("Scope type is invalid.");
        }

        if (request.WordOrder is null || !WordOrder.All.Contains(request.WordOrder))
        {
            return Result<bool>.Fail("Word order is invalid.");
        }

        if (request.AnswerMethod is null || !AnswerMethod.All.Contains(request.AnswerMethod))
        {
            return Result<bool>.Fail("Answer method is invalid.");
        }

        if (request.WordLimit <= 0)
        {
            return Result<bool>.Fail("Word limit must be greater than zero.");
        }

        if (request.Mode == TestMode.Timed && !request.TimeLimitSec.HasValue)
        {
            return Result<bool>.Fail("Timed mode requires time_limit_sec.");
        }

        if (request.Mode == TestMode.Elimination && !request.Lives.HasValue)
        {
            return Result<bool>.Fail("Elimination mode requires lives.");
        }

        return Result<bool>.Ok(true);
    }

    private static Result<CreateSessionResponse> ToCreateSessionFailure<T>(Result<T> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Result<CreateSessionResponse>.Unauthorized(result.Error!),
            StatusCodes.Status403Forbidden => Result<CreateSessionResponse>.Forbidden(result.Error!),
            StatusCodes.Status404NotFound => Result<CreateSessionResponse>.NotFound(result.Error!),
            StatusCodes.Status409Conflict => Result<CreateSessionResponse>.Conflict(result.Error!),
            _ => Result<CreateSessionResponse>.Fail(result.Error!),
        };
    }
}
