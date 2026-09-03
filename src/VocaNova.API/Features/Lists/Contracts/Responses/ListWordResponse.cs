using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Responses;

public sealed record ListWordResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("note")] string? Note,
    [property: JsonPropertyName("added_at")] DateTime AddedAt);
