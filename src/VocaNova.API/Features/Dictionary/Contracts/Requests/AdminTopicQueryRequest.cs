using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed class AdminTopicQueryRequest
{
    [JsonPropertyName("q")] public string? Q { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("includeDeleted")] public bool IncludeDeleted { get; set; }
}
