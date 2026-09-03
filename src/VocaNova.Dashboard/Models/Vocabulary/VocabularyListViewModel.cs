using VocaNova.Dashboard.Data.Dtos.Dictionary;

namespace VocaNova.Dashboard.Models.Vocabulary;

public sealed class VocabularyListViewModel
{
    public IReadOnlyList<WordListItem> Items { get; init; } = Array.Empty<WordListItem>();

    public IReadOnlyList<TopicSummary> Topics { get; init; } = Array.Empty<TopicSummary>();

    // Bộ lọc hiện tại (giữ lại để render form + link phân trang).
    public string? Q { get; init; }

    public string? Cefr { get; init; }

    public uint? TopicId { get; init; }

    public string? Status { get; init; }

    public string? WordType { get; init; }

    public bool IncludeDeleted { get; init; }

    /// <summary>Cột đang sắp xếp: word | type | cefr | phonetic | status. Null = mặc định (theo bảng chữ cái).</summary>
    public string? SortBy { get; init; }

    /// <summary>asc | desc.</summary>
    public string? SortDirection { get; init; }

    public int Page { get; init; } = 1;

    public int Limit { get; init; } = 10;

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static readonly IReadOnlyList<string> CefrLevels =
        new[] { "A1", "A2", "B1", "B2", "C1", "C2" };

    public static readonly IReadOnlyList<string> WordTypes =
        new[] { "noun", "verb", "adjective", "adverb", "preposition", "pronoun", "conjunction", "phrase" };
}
