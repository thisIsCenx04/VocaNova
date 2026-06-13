using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizSubmitRepository
{
    Task<TestSession?> FindSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default);

    Task<TestAnswer> UpsertAnswerAsync(
        TestSession session,
        QuestionDto question,
        SubmitAnswerRequest request,
        bool isCorrect,
        float? aiScore,
        string? aiExplanation,
        string? aiSuggestion,
        CancellationToken cancellationToken = default);

    Task CompleteSessionAsync(
        TestSession session,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
