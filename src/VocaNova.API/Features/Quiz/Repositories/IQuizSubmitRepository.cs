using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizSubmitRepository
{
    Task<TestSession?> FindSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chỉ dựng thay đổi trong bộ nhớ; người gọi chịu trách nhiệm
    /// <see cref="SaveChangesAsync"/> để cả thao tác nằm trong một transaction.
    /// </summary>
    TestAnswer UpsertAnswer(
        TestSession session,
        QuestionDto question,
        SubmitAnswerRequest request,
        bool isCorrect,
        float? aiScore,
        string? aiExplanation,
        string? aiSuggestion);

    /// <inheritdoc cref="UpsertAnswer"/>
    void CompleteSession(TestSession session);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
