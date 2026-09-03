using VocaNova.API.Features.Quiz.BLL.Models;

namespace VocaNova.API.Features.Quiz.BLL.Abstractions;

public interface IQuizQuestionRepository
{
    Task<QuizQuestionWord?> FindQuestionWordAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<QuizQuestionWord>> GetDistractorsAsync(
        uint excludedWordId, string wordClass, IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default);
}
