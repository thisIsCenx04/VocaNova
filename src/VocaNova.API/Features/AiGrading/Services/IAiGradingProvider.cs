using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

public interface IAiGradingProvider
{
    Task<AiGradingResult> GradeAsync(
        uint wordId,
        int questionType,
        string? userAnswer,
        string expectedAnswer,
        CancellationToken cancellationToken = default);
}
