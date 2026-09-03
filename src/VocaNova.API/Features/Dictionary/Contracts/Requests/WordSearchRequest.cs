using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class WordSearchRequest
{
    [JsonPropertyName("q")]
    public string? Q { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;

    [JsonPropertyName("cefr")]
    public string? Cefr { get; set; }

    [JsonPropertyName("topicId")]
    public uint? TopicId { get; set; }

    [JsonPropertyName("isPhrase")]
    public bool? IsPhrase { get; set; }
}
