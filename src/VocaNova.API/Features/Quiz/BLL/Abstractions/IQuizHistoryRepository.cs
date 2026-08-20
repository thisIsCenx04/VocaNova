using VocaNova.API.Common.Models;
using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizHistoryRepository
{
    Task<PagedCollection<QuizHistoryItem>> GetHistoryAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default);
    Task<PagedCollection<WrongWord>> GetWrongWordsAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default);
    Task<bool> ClearWrongWordAsync(uint userId, uint wordId, CancellationToken cancellationToken = default);
}
