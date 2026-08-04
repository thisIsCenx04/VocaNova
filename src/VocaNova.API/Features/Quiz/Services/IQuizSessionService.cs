using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizSessionService
{
    Task<Result<CreateSessionResponse>> CreateSessionAsync(
        uint userId,
        CreateSessionRequest request,
        CancellationToken cancellationToken = default);
}
