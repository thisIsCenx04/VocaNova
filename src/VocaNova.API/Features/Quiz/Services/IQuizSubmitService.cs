using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizSubmitService
{
    Task<Result<AnswerResultDto>> SubmitAnswerAsync(
        uint userId,
        uint sessionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken = default);
}
