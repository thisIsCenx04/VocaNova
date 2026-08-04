using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record AddTopicWordsRequest(
    [property: JsonPropertyName("word_ids")] IReadOnlyCollection<uint>? WordIds);
