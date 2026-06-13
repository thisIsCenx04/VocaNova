using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class ExactTypingGrader : IAnswerGrader
{
    public string AnswerMethod => Common.Constants.AnswerMethod.ExactTyping;

    public Task<GradeResult> GradeAsync(
        string? userAnswer,
        string expectedAnswer,
        IReadOnlyCollection<string>? acceptedAnswers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return Task.FromResult(new GradeResult(false));
        }

        var normalizedAnswer = userAnswer.NormalizeAnswer();
        var isCorrect = normalizedAnswer == expectedAnswer.NormalizeAnswer()
            || acceptedAnswers is not null
                && acceptedAnswers.Any(answer => !string.IsNullOrWhiteSpace(answer)
                    && normalizedAnswer == answer.NormalizeAnswer());

        return Task.FromResult(new GradeResult(isCorrect));
    }
}
