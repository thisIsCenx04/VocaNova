using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Repositories;

public interface IQuizQuestionRepository
{
    Task<QuizQuestionWordDto?> FindQuestionWordAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<QuizQuestionWordDto>> GetDistractorCandidatesAsync(
        uint excludingWordId,
        string wordClass,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default);
}
