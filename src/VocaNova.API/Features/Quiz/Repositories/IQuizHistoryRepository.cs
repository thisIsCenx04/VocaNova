using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizHistoryRepository
{
    Task<PagedResult<QuizHistoryItemDto>> GetHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WrongWordDto>> GetWrongWordsAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> ClearWrongWordAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
