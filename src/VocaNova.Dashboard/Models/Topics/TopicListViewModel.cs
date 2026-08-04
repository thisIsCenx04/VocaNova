using System.ComponentModel.DataAnnotations;
using VocaNova.Dashboard.Models.Api.Dictionary;

namespace VocaNova.Dashboard.Models.Topics;

public sealed class TopicCreateViewModel
{
    [StringLength(20, ErrorMessage = "The icon cannot exceed 20 characters.")]
    [RegularExpression(@"^bi bi-[a-z0-9-]+$", ErrorMessage = "The icon must be a valid Bootstrap Icon class.")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "The English topic name is required.")]
    [StringLength(50, ErrorMessage = "The English topic name cannot exceed 50 characters.")]
    public string? TopicName { get; set; }

    [StringLength(50, ErrorMessage = "The Vietnamese topic name cannot exceed 50 characters.")]
    public string? TopicNameVi { get; set; }

    public List<string> Keywords { get; set; } = new();

    public List<uint> WordIds { get; set; } = new();
}

public sealed class TopicEditViewModel
{
    public uint TopicId { get; set; }

    [StringLength(20, ErrorMessage = "The icon cannot exceed 20 characters.")]
    [RegularExpression(@"^bi bi-[a-z0-9-]+$", ErrorMessage = "The icon must be a valid Bootstrap Icon class.")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "The English topic name is required.")]
    [StringLength(50, ErrorMessage = "The English topic name cannot exceed 50 characters.")]
    public string? TopicName { get; set; }

    [StringLength(50, ErrorMessage = "The Vietnamese topic name cannot exceed 50 characters.")]
    public string? TopicNameVi { get; set; }

    public List<string> Keywords { get; set; } = new();

    public List<uint> WordIds { get; set; } = new();

    /// <summary>
    /// Chi tiết từ (loại từ, CEFR, phiên âm, trạng thái) để render bảng giống Word Management.
    /// Rỗng khi form post lại sau lỗi validation — lúc đó bảng dựng lại từ Keywords/WordIds.
    /// </summary>
    public IReadOnlyList<WordListItem> Words { get; set; } = Array.Empty<WordListItem>();

    public string? Q { get; set; }
    public string? Cefr { get; set; }
    public string? Status { get; set; }
    public string? WordType { get; set; }
    public bool IncludeDeleted { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Cột đang sort của bảng từ vựng (word | type | cefr | phonetic | status).</summary>
    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}

public sealed class TopicListViewModel
{
    public IReadOnlyList<AdminTopic> Items { get; init; } = Array.Empty<AdminTopic>();

    public string? Q { get; init; }

    public string? Status { get; init; }

    public bool IncludeDeleted { get; init; }

    public int Page { get; init; } = 1;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Cột đang sort (id | name | words | status).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed class TopicDetailViewModel
{
    public required AdminTopic Topic { get; init; }
    public bool IsAddingWord { get; init; }
    public IReadOnlyList<WordListItem> Items { get; init; } = Array.Empty<WordListItem>();
    public string? Q { get; init; }
    public string? Cefr { get; init; }
    public string? Status { get; init; }
    public string? WordType { get; init; }
    public bool IncludeDeleted { get; init; }
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Cột đang sort (word | type | cefr | phonetic | status).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
