using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed record AddPersonalTopicWordRequest(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("note")] string? Note);
