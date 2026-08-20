using VocaNova.API.Common.Constants;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class QuizSubmissionService : IQuizSubmissionService
{
    private readonly IQuizSubmissionRepository _repository;
    private readonly IQuizSessionBuilder _sessionBuilder;
    private readonly IQuizQuestionBuilder _questionBuilder;
    private readonly IReadOnlyDictionary<string, IAnswerGrader> _graders;
    private readonly IAiGradingService _aiGradingService;
    private readonly ISrsService _srsService;
    private readonly IProgressSummaryCache? _progressCache;
    private readonly IQuizPoolCache? _poolCache;

    public QuizSubmissionService(IQuizSubmissionRepository repository,
        IQuizSessionBuilder sessionBuilder, IQuizQuestionBuilder questionBuilder,
        IEnumerable<IAnswerGrader> graders, IAiGradingService aiGradingService,
        ISrsService srsService, IProgressSummaryCache? progressCache = null,
        IQuizPoolCache? poolCache = null)
    {
        _repository = repository;
        _sessionBuilder = sessionBuilder;
        _questionBuilder = questionBuilder;
        _graders = graders.ToDictionary(grader => grader.AnswerMethod, StringComparer.Ordinal);
        _aiGradingService = aiGradingService;
        _srsService = srsService;
        _progressCache = progressCache;
        _poolCache = poolCache;
    }

    public async Task<QuizOperationResult<QuizAnswer>> SubmitAnswerAsync(
        uint userId, uint sessionId, SubmitAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<QuizAnswer>.Unauthorized("Unauthorized.");
        if (sessionId == 0) return QuizOperationResult<QuizAnswer>.NotFound("Session not found.");
        if (command.WordId == 0) return QuizOperationResult<QuizAnswer>.NotFound("Word not found.");

        var session = await _repository.GetStateAsync(userId, sessionId, command.WordId, cancellationToken);
        if (session is null) return QuizOperationResult<QuizAnswer>.NotFound("Session not found.");
        if (session.Status != TestSessionStatus.InProgress)
            return QuizOperationResult<QuizAnswer>.Conflict("Quiz session is not in progress.");

        var poolResult = await BuildPoolFromSessionAsync(session, command.ListId, cancellationToken);
        if (!poolResult.IsSuccess)
            return QuizSessionService.FailureFrom<QuizAnswer, IReadOnlyCollection<QuizPoolWord>>(poolResult);
        var pool = poolResult.Value!;
        if (!pool.Any(word => word.WordId == command.WordId))
            return QuizOperationResult<QuizAnswer>.ValidationFailure("Word is not in this quiz session.");

        var questionResult = await _questionBuilder.BuildQuestionAsync(command.WordId, session.QuestionType, cancellationToken);
        if (!questionResult.IsSuccess)
            return QuizSessionService.FailureFrom<QuizAnswer, QuizQuestion>(questionResult);
        var question = questionResult.Value!;

        var gradeResult = await GradeAsync(session, command, question, cancellationToken);
        if (!gradeResult.IsSuccess)
            return QuizSessionService.FailureFrom<QuizAnswer, SubmissionGrade>(gradeResult);

        var answer = UpsertAnswer(session, question, command, gradeResult.Value!);
        var srsResult = await _srsService.UpdateProgressAsync(
            userId, command.WordId, gradeResult.Value!.IsCorrect, cancellationToken);
        if (!srsResult.IsSuccess)
            return QuizSessionService.FailureFrom<QuizAnswer, UserWordProgress>(srsResult);

        var nextQuestion = await BuildNextQuestionAsync(session, pool, cancellationToken);
        if (nextQuestion is null)
        {
            QuizSessionStatisticsCalculator.ApplyStats(session);
            session.Status = TestSessionStatus.Completed;
            session.EndedAt ??= DateTime.UtcNow;
            if (_poolCache is not null)
                await _poolCache.RemoveAsync(session.SessionId, command.ListId, cancellationToken);
        }

        // This is the single relational save for answer, session and SRS changes.
        // AI cache hit/write persistence may already have completed independently above.
        await _repository.SaveSubmissionAsync(
            new QuizSubmissionChanges(session, answer, srsResult.Value!), cancellationToken);

        if (_progressCache is not null) await _progressCache.RemoveAsync(userId, cancellationToken);

        return QuizOperationResult<QuizAnswer>.Success(new QuizAnswer(
            session.SessionId, command.WordId, gradeResult.Value.IsCorrect,
            question.ExpectedAnswer, session.CorrectCount, session.WrongCount, session.Score,
            gradeResult.Value.AiScore, gradeResult.Value.AiExplanation,
            gradeResult.Value.AiSuggestion, nextQuestion));
    }

    private async Task<QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>> BuildPoolFromSessionAsync(
        QuizSubmissionState session, uint? listId, CancellationToken cancellationToken)
    {
        if (_poolCache is not null)
        {
            var cached = await _poolCache.GetAsync(session.SessionId, listId, cancellationToken);
            if (cached is { Count: > 0 })
                return QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>.Success(cached);
        }

        var result = await _sessionBuilder.BuildPoolAsync(session.UserId,
            new BuildQuizPoolCommand(session.ScopeType, session.ScopeDateFrom, session.ScopeDateTo,
                session.TopicIds, session.WordOrder, null, session.AnswerMethod, listId), cancellationToken);
        if (result.IsSuccess && _poolCache is not null)
            await _poolCache.SetAsync(session.SessionId, listId, result.Value!, cancellationToken);
        return result;
    }

    private async Task<QuizOperationResult<SubmissionGrade>> GradeAsync(
        QuizSubmissionState session, SubmitAnswerCommand command, QuizQuestion question,
        CancellationToken cancellationToken)
    {
        if (session.AnswerMethod == AnswerMethod.AiTyping)
        {
            var grade = await _aiGradingService.GradeAsync(command.WordId, session.QuestionType,
                command.UserAnswer, question.ExpectedAnswer, cancellationToken);
            return QuizOperationResult<SubmissionGrade>.Success(new SubmissionGrade(
                grade.IsCorrect, grade.Score, grade.Explanation, grade.Suggestion));
        }

        if (!_graders.TryGetValue(session.AnswerMethod, out var grader))
            return QuizOperationResult<SubmissionGrade>.ValidationFailure("Answer method is unsupported.");

        var existing = session.Answers.SingleOrDefault(answer => answer.WordId == command.WordId);
        var accepted = AcceptedAnswersParser.Parse(existing?.AcceptedAnswersJson);
        var gradeResult = await grader.GradeAsync(command.UserAnswer, question.ExpectedAnswer, accepted, cancellationToken);
        return QuizOperationResult<SubmissionGrade>.Success(new SubmissionGrade(gradeResult.IsCorrect, null, null, null));
    }

    private static QuizSubmissionAnswer UpsertAnswer(QuizSubmissionState session,
        QuizQuestion question, SubmitAnswerCommand command, SubmissionGrade grade)
    {
        var answer = session.Answers.SingleOrDefault(item => item.WordId == command.WordId);
        if (answer is null)
        {
            answer = new QuizSubmissionAnswer
            {
                WordId = command.WordId,
                QuestionNumber = session.Answers.Count + 1,
            };
            session.Answers.Add(answer);
        }

        answer.SenseId = question.SenseId;
        answer.QuestionType = question.QuestionType;
        answer.DisplayContent = question.DisplayContent;
        answer.ExpectedAnswer = question.ExpectedAnswer;
        answer.UserAnswer = command.UserAnswer;
        answer.IsCorrect = grade.IsCorrect;
        answer.AiScore = grade.AiScore;
        answer.AiExplanation = grade.AiExplanation;
        answer.AiSuggestion = grade.AiSuggestion;
        QuizSessionStatisticsCalculator.ApplyStats(session);
        return answer;
    }

    private async Task<QuizQuestion?> BuildNextQuestionAsync(QuizSubmissionState session,
        IReadOnlyCollection<QuizPoolWord> pool, CancellationToken cancellationToken)
    {
        var answeredIds = session.Answers.Where(answer => answer.IsCorrect.HasValue)
            .Select(answer => answer.WordId).ToHashSet();
        if (answeredIds.Count >= session.QuestionCount) return null;
        foreach (var word in pool.Where(word => !answeredIds.Contains(word.WordId)))
        {
            var result = await _questionBuilder.BuildQuestionAsync(word.WordId, session.QuestionType, cancellationToken);
            if (result.IsSuccess) return result.Value;
        }
        return null;
    }

    private sealed record SubmissionGrade(bool IsCorrect, float? AiScore,
        string? AiExplanation, string? AiSuggestion);
}
