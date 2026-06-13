namespace VocaNova.API.Features.AiGrading.Services;

public interface IGeminiClient
{
    Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
