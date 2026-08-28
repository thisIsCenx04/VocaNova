using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Services.IServices;

public interface IQuizSessionBuilder
{
    Task<QuizOperationResult<IReadOnlyCollection<QuizPoolWord>>> BuildPoolAsync(
        uint userId, BuildQuizPoolCommand command, CancellationToken cancellationToken = default);
}
