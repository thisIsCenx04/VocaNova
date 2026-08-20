using System.Text.Json.Serialization;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Infrastructure.Caching.Lists;

internal sealed record UserListCacheEntry(
    [property: JsonPropertyName("list_id")] uint ListId,
    [property: JsonPropertyName("list_name")] string ListName,
    [property: JsonPropertyName("word_count")] int WordCount,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt)
{
    public static UserListCacheEntry FromBusinessModel(UserListSummary list) =>
        new(list.ListId, list.ListName, list.WordCount, list.CreatedAt);

    public UserListSummary ToBusinessModel() =>
        new(ListId, ListName, WordCount, CreatedAt);
}
