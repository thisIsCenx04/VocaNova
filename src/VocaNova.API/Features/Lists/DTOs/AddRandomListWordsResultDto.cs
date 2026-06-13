using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed record AddRandomListWordsResultDto(
    [property: JsonPropertyName("added_count")] int AddedCount,
    [property: JsonPropertyName("words")] IReadOnlyCollection<ListWordDto> Words);
