using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizQuestionBuilder
{
    Task<Result<QuestionDto>> BuildQuestionAsync(
        uint wordId,
        int questionType,
        CancellationToken cancellationToken = default);
}
