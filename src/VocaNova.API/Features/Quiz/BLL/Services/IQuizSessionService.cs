using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public interface IQuizSessionService
{
    Task<QuizOperationResult<CreatedQuizSession>> CreateSessionAsync(
        uint userId, CreateQuizSessionCommand command, CancellationToken cancellationToken = default);
}
