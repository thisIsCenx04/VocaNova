using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizResultRepository
{
    Task<QuizResultState?> GetSessionAsync(uint userId, uint sessionId, CancellationToken cancellationToken = default);
    Task SaveFinishAsync(QuizFinishChanges changes, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TestAnswerResult>> GetAnswersAsync(uint sessionId, CancellationToken cancellationToken = default);
}
