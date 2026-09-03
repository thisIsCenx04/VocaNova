using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;

namespace VocaNova.Tests.Quiz;

internal sealed class StubAiGradingService : IAiGradingService
{
    public Task<AiGrade> GradeAsync(uint wordId, int questionType, string? userAnswer,
        string expectedAnswer, CancellationToken cancellationToken = default)
    {
        var score = !string.IsNullOrWhiteSpace(userAnswer)
            && userAnswer.NormalizeAnswer() == expectedAnswer.NormalizeAnswer() ? 1f : 0f;
        var correct = score >= AppSettings.AiPassThreshold;
        return Task.FromResult(new AiGrade(correct, score, "Stub AI grading result.",
            correct ? null : expectedAnswer));
    }
}
