using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed record UserListDto(
    [property: JsonPropertyName("list_id")] uint ListId,
    [property: JsonPropertyName("list_name")] string ListName,
    [property: JsonPropertyName("word_count")] int WordCount,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);
