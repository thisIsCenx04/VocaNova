using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface ISrsService
{
    Task<Result<UserWordProgressDto>> UpdateProgressAsync(
        uint userId,
        uint wordId,
        bool isCorrect,
        CancellationToken cancellationToken = default);
}
