using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class AdminWordQueryRequest
{
    [JsonPropertyName("q")] public string? Q { get; set; }
    [JsonPropertyName("cefr")] public string? Cefr { get; set; }
    [JsonPropertyName("topicId")] public uint? TopicId { get; set; }
    [JsonPropertyName("wordType")] public string? WordType { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("includeDeleted")] public bool IncludeDeleted { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; } = 1;
    [JsonPropertyName("limit")] public int Limit { get; set; } = 20;
    [JsonPropertyName("sortBy")] public string? SortBy { get; set; }
    [JsonPropertyName("sortDirection")] public string? SortDirection { get; set; }
}
