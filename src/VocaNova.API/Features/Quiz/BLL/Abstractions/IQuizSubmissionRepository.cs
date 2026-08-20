using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizSubmissionRepository
{
    Task<QuizSubmissionState?> GetStateAsync(uint userId, uint sessionId, uint wordId,
        CancellationToken cancellationToken = default);
    Task SaveSubmissionAsync(QuizSubmissionChanges changes, CancellationToken cancellationToken = default);
}
