namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record AiGradingResult(
    bool IsCorrect,
    float Score,
    string Explanation,
    string? Suggestion);
