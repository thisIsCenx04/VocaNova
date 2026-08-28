using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizSessionRepository
{
    Task<QuizSession> CreateAsync(uint userId, CreateQuizSessionCommand command,
        IReadOnlyCollection<uint> topicIds, int questionCount,
        CancellationToken cancellationToken = default);
}
