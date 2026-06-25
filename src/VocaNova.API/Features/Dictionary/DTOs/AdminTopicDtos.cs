using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

/// <summary>Bộ lọc danh sách topic cho dashboard admin (F061).</summary>
public sealed class AdminTopicQuery
{
    [JsonPropertyName("q")]
    public string? Q { get; set; }

    /// <summary>active | deleted. Null = không lọc theo status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>true = dùng IgnoreQueryFilters để thấy cả topic đã xóa.</summary>
    [JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; set; }
}

/// <summary>Một dòng quản lý topic. <c>word_count</c> = số từ active đang dùng topic (khớp guard xóa).</summary>
public sealed record AdminTopicDto(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("topic_name")] string TopicName,
    [property: JsonPropertyName("topic_name_vi")] string? TopicNameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("word_count")] int WordCount);
