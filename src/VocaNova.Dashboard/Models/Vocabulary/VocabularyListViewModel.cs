using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.Vocabulary;

/// <summary>Tham số filter/paging bind từ query string của trang vocabulary.</summary>
public sealed class VocabularyListQuery
{
    public string? Q { get; set; }

    public string? Cefr { get; set; }

    public uint? TopicId { get; set; }

    public string? Status { get; set; }

    public bool IncludeDeleted { get; set; }

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 20;
}

public sealed class VocabularyListViewModel
{
    public VocabularyListQuery Query { get; init; } = new();

    public IReadOnlyList<WordSummaryDto> Words { get; init; } = Array.Empty<WordSummaryDto>();

    public bool Loaded { get; init; }

    public PaginationInfo? Pagination { get; init; }

    public IReadOnlyList<TopicSummaryDto> Topics { get; init; } = Array.Empty<TopicSummaryDto>();

    /// <summary>true khi admin word list (G1) đã có → cho phép filter status + toggle "đã xóa".</summary>
    public bool AdminListAvailable { get; init; }
}
