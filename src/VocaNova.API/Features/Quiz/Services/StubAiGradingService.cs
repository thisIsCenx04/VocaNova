using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Features.AiGrading.Services;
using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public sealed class StubAiGradingService : IAiGradingService, IAiGradingProvider
{
    public Task<AiGradingResult> GradeAsync(
        uint wordId,
        int questionType,
        string? userAnswer,
        string expectedAnswer,
        CancellationToken cancellationToken = default)
    {
        var score = !string.IsNullOrWhiteSpace(userAnswer)
            && userAnswer.NormalizeAnswer() == expectedAnswer.NormalizeAnswer()
                ? 1f
                : 0f;

        var isCorrect = score >= AppSettings.AiPassThreshold;
        return Task.FromResult(new AiGradingResult(
            isCorrect,
            score,
            "Stub AI grading result.",
            isCorrect ? null : expectedAnswer));
    }
}
