namespace VocaNova.API.Features.AiGrading.BLL.Models;

public sealed record AiGrade(
    bool IsCorrect,
    float Score,
    string Explanation,
    string? Suggestion,
    bool FromAi = true);

public sealed record AiGradeRequest(
    uint WordId,
    int QuestionType,
    string? UserAnswer,
    string ExpectedAnswer);

public sealed record AiGradeCacheKey(
    string Value,
    uint WordId,
    int QuestionType,
    string NormalizedUserAnswer,
    string ExpectedAnswer);

public sealed record AiGradingConfigurationView(
    string Provider,
    string Endpoint,
    string Model,
    IReadOnlyList<string> FallbackModels,
    int MaxAttempts,
    int RetryBaseDelayMs,
    int AttemptTimeoutSeconds,
    double PassThreshold,
    bool HasApiKey,
    string? ApiKeyHint,
    string Storage,
    bool CanWriteEnvFile,
    IReadOnlyList<string> SupportedProviders);

public sealed record UpdateAiGradingConfigurationCommand(
    string? Provider,
    string? Endpoint,
    string? Model,
    IReadOnlyList<string>? FallbackModels,
    string? ApiKey,
    int? MaxAttempts,
    int? RetryBaseDelayMs,
    int? AttemptTimeoutSeconds,
    double? PassThreshold);

public sealed record AiGradingConnectionTest(
    bool Succeeded,
    string Model,
    long ElapsedMs,
    string Message);
