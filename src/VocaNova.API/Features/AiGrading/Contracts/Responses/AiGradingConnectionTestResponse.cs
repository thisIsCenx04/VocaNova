using System.Text.Json.Serialization;

namespace VocaNova.API.Features.AiGrading.Contracts.Responses;

public sealed record AiGradingConnectionTestResponse(
    [property: JsonPropertyName("succeeded")] bool Succeeded,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("elapsed_ms")] long ElapsedMs,
    [property: JsonPropertyName("message")] string Message);
