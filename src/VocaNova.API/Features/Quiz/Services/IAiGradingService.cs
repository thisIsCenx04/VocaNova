using VocaNova.API.Features.Quiz.DTOs;

namespace VocaNova.API.Features.Quiz.Services;

public interface IAiGradingService
{
    Task<AiGradingResult> GradeAsync(
        string? userAnswer,
        string expectedAnswer,
        CancellationToken cancellationToken = default);
}
