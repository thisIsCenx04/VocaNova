namespace VocaNova.API.Features.AiGrading.Services;

public interface IGeminiClient
{
    Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a prompt against explicit settings instead of the ones currently in force. Used by
    /// the admin "test connection" action so unsaved credentials can be verified first.
    /// </summary>
    Task<string> GenerateContentAsync(
        string prompt,
        AiGradingSettings settings,
        CancellationToken cancellationToken = default);
}
