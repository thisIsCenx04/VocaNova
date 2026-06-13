using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record GradeResult(
    [property: JsonPropertyName("is_correct")] bool IsCorrect);
