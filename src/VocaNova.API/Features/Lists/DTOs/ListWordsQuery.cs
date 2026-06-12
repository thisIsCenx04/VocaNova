using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed class ListWordsQuery
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}
