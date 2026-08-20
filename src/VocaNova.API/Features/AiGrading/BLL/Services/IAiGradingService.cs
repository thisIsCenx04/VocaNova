using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Features.AiGrading.BLL.Services;

public interface IAiGradingService
{
    Task<AiGrade> GradeAsync(uint wordId, int questionType, string? userAnswer,
        string expectedAnswer, CancellationToken cancellationToken = default);
}
