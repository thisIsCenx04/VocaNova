using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.DTOs;

public sealed record MasteryBreakdownDto(
    [property: JsonPropertyName("mastery_level")] int MasteryLevel,
    [property: JsonPropertyName("word_count")] int WordCount);
