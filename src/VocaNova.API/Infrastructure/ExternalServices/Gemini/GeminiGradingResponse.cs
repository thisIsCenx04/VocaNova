using System.Text.Json.Serialization;

namespace VocaNova.API.Infrastructure.ExternalServices.Gemini;

public sealed record GeminiGradingResponse(
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("explanation")] string Explanation,
    [property: JsonPropertyName("suggestion")] string? Suggestion);
