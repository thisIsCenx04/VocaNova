using VocaNova.API.Common.Models;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services;

public interface IQuizHistoryService
{
    Task<QuizOperationResult<PagedCollection<QuizHistoryItem>>> GetHistoryAsync(uint userId, QuizHistoryQuery query, CancellationToken cancellationToken = default);
    Task<QuizOperationResult<PagedCollection<WrongWord>>> GetWrongWordsAsync(uint userId, WrongWordsQuery query, CancellationToken cancellationToken = default);
    Task<QuizOperationResult<bool>> ClearWrongWordAsync(uint userId, uint wordId, CancellationToken cancellationToken = default);
}
