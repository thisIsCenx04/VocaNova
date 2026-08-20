using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class QuizSessionService : IQuizSessionService
{
    private const string NotEnoughWordsMessage = "Không đủ từ để tạo bài kiểm tra";
    private static readonly IReadOnlySet<int> QuestionTypes = new HashSet<int>
    {
        int.Parse(QuestionType.WordToMeaning),
        int.Parse(QuestionType.MeaningToWord),
        int.Parse(QuestionType.Description),
    };

    private readonly IQuizSessionBuilder _sessionBuilder;
    private readonly IQuizQuestionBuilder _questionBuilder;
    private readonly IQuizSessionRepository _repository;
    private readonly IProgressSummaryCache? _progressCache;

    public QuizSessionService(IQuizSessionBuilder sessionBuilder, IQuizQuestionBuilder questionBuilder,
        IQuizSessionRepository repository, IProgressSummaryCache? progressCache = null)
    {
        _sessionBuilder = sessionBuilder;
        _questionBuilder = questionBuilder;
        _repository = repository;
        _progressCache = progressCache;
    }

    public async Task<QuizOperationResult<CreatedQuizSession>> CreateSessionAsync(
        uint userId, CreateQuizSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (userId == 0) return QuizOperationResult<CreatedQuizSession>.Unauthorized("Unauthorized.");
        var error = Validate(command);
        if (error is not null) return QuizOperationResult<CreatedQuizSession>.ValidationFailure(error);

        var poolResult = await _sessionBuilder.BuildPoolAsync(userId, new BuildQuizPoolCommand(
            command.ScopeType!, command.ScopeDateFrom, command.ScopeDateTo, command.TopicIds,
            command.WordOrder!, command.WordLimit, command.AnswerMethod!, command.ListId), cancellationToken);
        if (!poolResult.IsSuccess) return FailureFrom<CreatedQuizSession, IReadOnlyCollection<QuizPoolWord>>(poolResult);

        var pool = poolResult.Value!;
        if (pool.Count == 0) return QuizOperationResult<CreatedQuizSession>.ValidationFailure(NotEnoughWordsMessage);

        var questionResult = await _questionBuilder.BuildQuestionAsync(
            pool.First().WordId, command.QuestionType, cancellationToken);
        if (!questionResult.IsSuccess) return FailureFrom<CreatedQuizSession, QuizQuestion>(questionResult);

        var topicIds = command.TopicIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
        var session = await _repository.CreateAsync(userId, command, topicIds, pool.Count, cancellationToken);
        if (_progressCache is not null) await _progressCache.RemoveAsync(userId, cancellationToken);

        return QuizOperationResult<CreatedQuizSession>.Success(new CreatedQuizSession(session, questionResult.Value!));
    }

    private static string? Validate(CreateQuizSessionCommand command)
    {
        if (command.Mode is null || !TestMode.All.Contains(command.Mode)) return "Mode is invalid.";
        if (!QuestionTypes.Contains(command.QuestionType)) return "Question type is invalid.";
        if (command.ScopeType is null || !ScopeType.Values.Contains(command.ScopeType)) return "Scope type is invalid.";
        if (command.WordOrder is null || !WordOrder.All.Contains(command.WordOrder)) return "Word order is invalid.";
        if (command.AnswerMethod is null || !AnswerMethod.All.Contains(command.AnswerMethod)) return "Answer method is invalid.";
        if (command.WordLimit <= 0) return "Word limit must be greater than zero.";
        if (command.Mode == TestMode.Timed && !command.TimeLimitSec.HasValue) return "Timed mode requires time_limit_sec.";
        if (command.Mode == TestMode.Elimination && !command.Lives.HasValue) return "Elimination mode requires lives.";
        return null;
    }

    internal static QuizOperationResult<TTarget> FailureFrom<TTarget, TSource>(QuizOperationResult<TSource> source) =>
        source.ErrorKind switch
        {
            QuizErrorKind.Unauthorized => QuizOperationResult<TTarget>.Unauthorized(source.Error!),
            QuizErrorKind.NotFound => QuizOperationResult<TTarget>.NotFound(source.Error!),
            QuizErrorKind.Forbidden => QuizOperationResult<TTarget>.Forbidden(source.Error!),
            QuizErrorKind.Conflict => QuizOperationResult<TTarget>.Conflict(source.Error!),
            _ => QuizOperationResult<TTarget>.ValidationFailure(source.Error!),
        };
}
