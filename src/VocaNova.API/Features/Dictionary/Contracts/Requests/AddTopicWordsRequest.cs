using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed record AddTopicWordsRequest(
    [property: JsonPropertyName("word_ids")] IReadOnlyCollection<uint>? WordIds);
