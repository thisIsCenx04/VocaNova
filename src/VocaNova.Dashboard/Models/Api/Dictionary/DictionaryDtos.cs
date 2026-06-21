namespace VocaNova.Dashboard.Models.Api.Dictionary;

// Map từ envelope API qua ApiJson.Default (SnakeCaseLower) — không cần [JsonPropertyName] vì không có field chứa số.

public sealed class WordSummaryDto
{
    public uint WordId { get; set; }

    public string Word { get; set; } = string.Empty;

    public string? Phonetic { get; set; }

    public string? Cefr { get; set; }

    public string? PrimaryMeaning { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Chỉ có khi dùng admin word list (G1); `GET /api/words` công khai không trả → null.</summary>
    public string? Status { get; set; }
}

public sealed class TopicSummaryDto
{
    public uint TopicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameVi { get; set; }

    public string? Icon { get; set; }

    public int WordCount { get; set; }
}
