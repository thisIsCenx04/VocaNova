using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IAnswerGrader
{
    string AnswerMethod { get; }

    Task<GradeResult> GradeAsync(
        string? userAnswer,
        string expectedAnswer,
        IReadOnlyCollection<string>? acceptedAnswers = null,
        CancellationToken cancellationToken = default);
}
