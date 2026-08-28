using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizPoolRepository
{
    Task<IReadOnlyCollection<QuizPoolWord>> GetCandidatesAsync(
        uint userId, BuildQuizPoolCommand command, CancellationToken cancellationToken = default);
}
