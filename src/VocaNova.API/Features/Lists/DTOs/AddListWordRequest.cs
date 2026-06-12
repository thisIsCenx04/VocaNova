using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed record AddListWordRequest(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("add_method")] string? AddMethod,
    [property: JsonPropertyName("note")] string? Note);
