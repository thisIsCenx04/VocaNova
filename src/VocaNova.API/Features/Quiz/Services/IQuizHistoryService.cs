using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizHistoryService
{
    Task<Result<PagedResult<QuizHistoryItemDto>>> GetHistoryAsync(
        uint userId,
        QuizHistoryQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<WrongWordDto>>> GetWrongWordsAsync(
        uint userId,
        WrongWordsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ClearWrongWordAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
