using Microsoft.AspNetCore.Http;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Repositories;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class QuizSubmitService : IQuizSubmitService
{
    private readonly IQuizSubmitRepository _quizSubmitRepository;
    private readonly IQuizSessionBuilder _quizSessionBuilder;
    private readonly IQuizQuestionBuilder _quizQuestionBuilder;
    private readonly IReadOnlyDictionary<string, IAnswerGrader> _answerGraders;
    private readonly IAiGradingService _aiGradingService;
    private readonly ISrsService _srsService;

    public QuizSubmitService(
        IQuizSubmitRepository quizSubmitRepository,
        IQuizSessionBuilder quizSessionBuilder,
        IQuizQuestionBuilder quizQuestionBuilder,
        IEnumerable<IAnswerGrader> answerGraders,
        IAiGradingService aiGradingService,
        ISrsService srsService)
    {
        _quizSubmitRepository = quizSubmitRepository;
        _quizSessionBuilder = quizSessionBuilder;
        _quizQuestionBuilder = quizQuestionBuilder;
        _answerGraders = answerGraders.ToDictionary(grader => grader.AnswerMethod, StringComparer.Ordinal);
        _aiGradingService = aiGradingService;
        _srsService = srsService;
    }

    public async Task<Result<AnswerResultDto>> SubmitAnswerAsync(
        uint userId,
        uint sessionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<AnswerResultDto>.Unauthorized("Unauthorized.");
        }

        if (sessionId == 0)
        {
            return Result<AnswerResultDto>.NotFound("Session not found.");
        }

        if (request.WordId == 0)
        {
            return Result<AnswerResultDto>.NotFound("Word not found.");
        }

        var session = await _quizSubmitRepository.FindSessionAsync(userId, sessionId, cancellationToken);
        if (session is null)
        {
            return Result<AnswerResultDto>.NotFound("Session not found.");
        }

        if (session.Status != TestSessionStatus.InProgress)
        {
            return Result<AnswerResultDto>.Conflict("Quiz session is not in progress.");
        }

        var poolResult = await BuildPoolFromSessionAsync(session, cancellationToken);
        if (!poolResult.IsSuccess)
        {
            return ToAnswerResultFailure(poolResult);
        }

        var pool = poolResult.Value!;
        if (!pool.Any(word => word.WordId == request.WordId))
        {
            return Result<AnswerResultDto>.Fail("Word is not in this quiz session.");
        }

        var questionResult = await _quizQuestionBuilder.BuildQuestionAsync(
            request.WordId,
            session.QuestionType,
            cancellationToken);
        if (!questionResult.IsSuccess)
        {
            return ToAnswerResultFailure(questionResult);
        }

        var question = questionResult.Value!;
        var grade = await GradeAsync(session, request, question, cancellationToken);
        if (!grade.IsSuccess)
        {
            return ToAnswerResultFailure(grade);
        }

        await _quizSubmitRepository.UpsertAnswerAsync(
            session,
            question,
            request,
            grade.Value!.IsCorrect,
            grade.Value.AiScore,
            grade.Value.AiExplanation,
            grade.Value.AiSuggestion,
            cancellationToken);

        var srsResult = await _srsService.UpdateProgressAsync(
            userId,
            request.WordId,
            grade.Value.IsCorrect,
            cancellationToken);
        if (!srsResult.IsSuccess)
        {
            return ToAnswerResultFailure(srsResult);
        }

        var nextQuestion = await BuildNextQuestionAsync(session, pool, cancellationToken);

        return Result<AnswerResultDto>.Ok(new AnswerResultDto(
            session.SessionId,
            request.WordId,
            grade.Value.IsCorrect,
            question.ExpectedAnswer,
            session.CorrectCount,
            session.WrongCount,
            session.Score,
            grade.Value.AiScore,
            grade.Value.AiExplanation,
            grade.Value.AiSuggestion,
            nextQuestion));
    }

    private async Task<Result<IReadOnlyCollection<QuizPoolWordDto>>> BuildPoolFromSessionAsync(
        TestSession session,
        CancellationToken cancellationToken)
    {
        return await _quizSessionBuilder.BuildPoolAsync(
            session.UserId,
            new BuildQuizPoolRequest(
                session.ScopeType,
                session.ScopeDateFrom,
                session.ScopeDateTo,
                session.TestSessionTopics.Select(topic => topic.TopicId).ToArray(),
                session.WordOrder,
                session.WordLimit,
                session.TestType),
            cancellationToken);
    }

    private async Task<Result<SubmitGradeResult>> GradeAsync(
        TestSession session,
        SubmitAnswerRequest request,
        QuestionDto question,
        CancellationToken cancellationToken)
    {
        if (session.TestType == AnswerMethod.AiTyping)
        {
            var aiResult = await _aiGradingService.GradeAsync(
                request.UserAnswer,
                question.ExpectedAnswer,
                cancellationToken);

            return Result<SubmitGradeResult>.Ok(new SubmitGradeResult(
                aiResult.IsCorrect,
                aiResult.Score,
                aiResult.Explanation,
                aiResult.Suggestion));
        }

        if (!_answerGraders.TryGetValue(session.TestType, out var grader))
        {
            return Result<SubmitGradeResult>.Fail("Answer method is unsupported.");
        }

        var existingAnswer = session.TestAnswers
            .SingleOrDefault(answer => answer.WordId == request.WordId);
        var acceptedAnswers = AcceptedAnswersJsonParser.Parse(existingAnswer?.AcceptedAnswers);
        var grade = await grader.GradeAsync(
            request.UserAnswer,
            question.ExpectedAnswer,
            acceptedAnswers,
            cancellationToken);

        return Result<SubmitGradeResult>.Ok(new SubmitGradeResult(
            grade.IsCorrect,
            null,
            null,
            null));
    }

    private async Task<QuestionDto?> BuildNextQuestionAsync(
        TestSession session,
        IReadOnlyCollection<QuizPoolWordDto> pool,
        CancellationToken cancellationToken)
    {
        var answeredWordIds = session.TestAnswers
            .Where(answer => answer.IsCorrect.HasValue)
            .Select(answer => answer.WordId)
            .ToHashSet();

        foreach (var nextWord in pool.Where(word => !answeredWordIds.Contains(word.WordId)))
        {
            var questionResult = await _quizQuestionBuilder.BuildQuestionAsync(
                nextWord.WordId,
                session.QuestionType,
                cancellationToken);
            if (questionResult.IsSuccess)
            {
                return questionResult.Value;
            }
        }

        return null;
    }

    private static Result<AnswerResultDto> ToAnswerResultFailure<T>(Result<T> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Result<AnswerResultDto>.Unauthorized(result.Error!),
            StatusCodes.Status403Forbidden => Result<AnswerResultDto>.Forbidden(result.Error!),
            StatusCodes.Status404NotFound => Result<AnswerResultDto>.NotFound(result.Error!),
            StatusCodes.Status409Conflict => Result<AnswerResultDto>.Conflict(result.Error!),
            _ => Result<AnswerResultDto>.Fail(result.Error!),
        };
    }

    private sealed record SubmitGradeResult(
        bool IsCorrect,
        float? AiScore,
        string? AiExplanation,
        string? AiSuggestion);
}
