using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Responses;

public sealed record AddRandomListWordsResponse(
    [property: JsonPropertyName("added_count")] int AddedCount,
    [property: JsonPropertyName("words")] IReadOnlyCollection<ListWordResponse> Words);
