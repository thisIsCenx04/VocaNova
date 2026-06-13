using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IQuizSessionBuilder
{
    Task<Result<IReadOnlyCollection<QuizPoolWordDto>>> BuildPoolAsync(
        uint userId,
        BuildQuizPoolRequest request,
        CancellationToken cancellationToken = default);
}
