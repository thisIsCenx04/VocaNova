using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Features.AiGrading.BLL.Abstractions;

public interface IAiGradingProvider
{
    Task<AiGrade> GradeAsync(AiGradeRequest request, CancellationToken cancellationToken = default);
    Task<AiGradingConnectionTest> TestConnectionAsync(
        AiGradingConfiguration configuration, CancellationToken cancellationToken = default);
}
