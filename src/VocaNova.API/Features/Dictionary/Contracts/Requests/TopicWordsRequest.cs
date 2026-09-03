using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class TopicWordsRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}
