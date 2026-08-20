using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public interface ISrsService
{
    Task<QuizOperationResult<UserWordProgress>> UpdateProgressAsync(
        uint userId, uint wordId, bool isCorrect, CancellationToken cancellationToken = default);
}
