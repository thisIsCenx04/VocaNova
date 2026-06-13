using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class MultipleChoiceGrader : IAnswerGrader
{
    public string AnswerMethod => Common.Constants.AnswerMethod.MultipleChoice;

    public Task<GradeResult> GradeAsync(
        string? userAnswer,
        string expectedAnswer,
        IReadOnlyCollection<string>? acceptedAnswers = null,
        CancellationToken cancellationToken = default)
    {
        if (userAnswer is null)
        {
            return Task.FromResult(new GradeResult(false));
        }

        var isCorrect = string.Equals(userAnswer, expectedAnswer, StringComparison.Ordinal)
            || acceptedAnswers is not null
                && acceptedAnswers.Any(answer => string.Equals(userAnswer, answer, StringComparison.Ordinal));

        return Task.FromResult(new GradeResult(isCorrect));
    }
}
