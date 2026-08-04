using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizResultService
{
    Task<Result<QuizResultDto>> FinishSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default);

    Task<Result<QuizResultDto>> GetResultAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default);
}
