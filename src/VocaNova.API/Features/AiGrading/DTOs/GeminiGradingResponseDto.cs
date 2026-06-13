using System.Text.Json.Serialization;

namespace VocaNova.API.Features.AiGrading.DTOs;

public sealed record GeminiGradingResponseDto(
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("explanation")] string Explanation,
    [property: JsonPropertyName("suggestion")] string? Suggestion);
