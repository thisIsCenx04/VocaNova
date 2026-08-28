using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services.IServices;

public interface IQuizSubmissionService
{
    Task<QuizOperationResult<QuizAnswer>> SubmitAnswerAsync(
        uint userId, uint sessionId, SubmitAnswerCommand command,
        CancellationToken cancellationToken = default);
}
