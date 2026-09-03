using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services.IServices;

public interface IQuizQuestionBuilder
{
    Task<QuizOperationResult<QuizQuestion>> BuildQuestionAsync(
        uint wordId, int questionType, CancellationToken cancellationToken = default);
}
