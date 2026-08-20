using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public sealed class QuizResultService : IQuizResultService
{
    private readonly IQuizResultRepository _repository;
    public QuizResultService(IQuizResultRepository repository) => _repository = repository;

    public async Task<QuizOperationResult<QuizResult>> FinishSessionAsync(
        uint userId, uint sessionId, CancellationToken cancellationToken = default)
    {
        var stateResult = await FindOwnedSessionAsync(userId, sessionId, cancellationToken);
        if (!stateResult.IsSuccess)
            return QuizSessionService.FailureFrom<QuizResult, QuizResultState>(stateResult);
        var state = stateResult.Value!;
        if (state.Status != TestSessionStatus.InProgress)
            return QuizOperationResult<QuizResult>.Conflict("Quiz session is not in progress.");

        QuizSessionStatisticsCalculator.ApplyStats(state);
        state.Status = TestSessionStatus.Abandoned;
        state.EndedAt = DateTime.UtcNow;
        await _repository.SaveFinishAsync(new QuizFinishChanges(state), cancellationToken);
        return QuizOperationResult<QuizResult>.Success(QuizSessionStatisticsCalculator.ToResult(state));
    }

    public async Task<QuizOperationResult<QuizResult>> GetResultAsync(
        uint userId, uint sessionId, CancellationToken cancellationToken = default)
    {
        var stateResult = await FindOwnedSessionAsync(userId, sessionId, cancellationToken);
        return stateResult.IsSuccess
            ? QuizOperationResult<QuizResult>.Success(QuizSessionStatisticsCalculator.ToResult(stateResult.Value!))
            : QuizSessionService.FailureFrom<QuizResult, QuizResultState>(stateResult);
    }

    private async Task<QuizOperationResult<QuizResultState>> FindOwnedSessionAsync(
        uint userId, uint sessionId, CancellationToken cancellationToken)
    {
        if (userId == 0) return QuizOperationResult<QuizResultState>.Unauthorized("Unauthorized.");
        if (sessionId == 0) return QuizOperationResult<QuizResultState>.NotFound("Session not found.");
        var session = await _repository.GetSessionAsync(userId, sessionId, cancellationToken);
        if (session is null) return QuizOperationResult<QuizResultState>.NotFound("Session not found.");
        var answers = await _repository.GetAnswersAsync(sessionId, cancellationToken);
        return QuizOperationResult<QuizResultState>.Success(new QuizResultState
        {
            SessionId = session.SessionId,
            Status = session.Status,
            QuestionCount = session.QuestionCount,
            CorrectCount = session.CorrectCount,
            WrongCount = session.WrongCount,
            Score = session.Score,
            MaxStreak = session.MaxStreak,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Answers = answers,
        });
    }
}
