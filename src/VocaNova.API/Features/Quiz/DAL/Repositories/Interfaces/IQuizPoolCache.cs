using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizPoolCache
{
    Task<IReadOnlyCollection<QuizPoolWord>?> GetAsync(uint sessionId, uint? listId, CancellationToken cancellationToken = default);
    Task SetAsync(uint sessionId, uint? listId, IReadOnlyCollection<QuizPoolWord> pool, CancellationToken cancellationToken = default);
    Task RemoveAsync(uint sessionId, uint? listId, CancellationToken cancellationToken = default);
}
