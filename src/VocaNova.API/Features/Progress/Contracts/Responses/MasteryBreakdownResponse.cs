using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.Contracts.Responses;

public sealed record MasteryBreakdownResponse(
    [property: JsonPropertyName("mastery_level")] int MasteryLevel,
    [property: JsonPropertyName("word_count")] int WordCount);
