using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizWordPoolRepository
{
    Task<IReadOnlyCollection<QuizPoolWordDto>> GetCandidatesAsync(
        uint userId,
        DateTime? addedFrom,
        DateTime? addedBefore,
        IReadOnlyCollection<uint>? topicIds,
        uint? listId,
        bool wrongWordsOnly = false,
        CancellationToken cancellationToken = default);
}
