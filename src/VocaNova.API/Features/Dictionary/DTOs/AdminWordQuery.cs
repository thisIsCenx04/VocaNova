using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

/// <summary>Bộ lọc danh sách từ vựng cho dashboard admin (F057): search, CEFR, topic, status, hiện đã xóa.</summary>
public sealed class AdminWordQuery
{
    [JsonPropertyName("q")]
    public string? Q { get; set; }

    [JsonPropertyName("cefr")]
    public string? Cefr { get; set; }

    [JsonPropertyName("topicId")]
    public uint? TopicId { get; set; }

    /// <summary>active | deleted. Null = không lọc theo status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>true = dùng IgnoreQueryFilters để thấy cả từ đã xóa.</summary>
    [JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}
