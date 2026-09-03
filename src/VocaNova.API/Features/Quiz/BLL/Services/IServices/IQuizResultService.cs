using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services.IServices;

public interface IQuizResultService
{
    Task<QuizOperationResult<QuizResult>> FinishSessionAsync(uint userId, uint sessionId, CancellationToken cancellationToken = default);
    Task<QuizOperationResult<QuizResult>> GetResultAsync(uint userId, uint sessionId, CancellationToken cancellationToken = default);
}
