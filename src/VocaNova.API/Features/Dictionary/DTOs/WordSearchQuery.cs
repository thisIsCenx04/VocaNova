using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed class WordSearchQuery
{
    public WordSearchQuery()
    {
    }

    public WordSearchQuery(
        string? q,
        int page = 1,
        int limit = 20,
        string? cefr = null,
        uint? topicId = null,
        bool? isPhrase = null)
    {
        Q = q;
        Page = page;
        Limit = limit;
        Cefr = cefr;
        TopicId = topicId;
        IsPhrase = isPhrase;
    }

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
