namespace VocaNova.API.Features.AiGrading.DTOs;

public sealed record CachedAiGradingResult(
    float Score,
    string? Explanation,
    string? Suggestion);
