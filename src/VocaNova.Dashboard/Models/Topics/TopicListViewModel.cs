using System.ComponentModel.DataAnnotations;
using VocaNova.Dashboard.Models.Api.Dictionary;

namespace VocaNova.Dashboard.Models.Topics;

public sealed class TopicCreateViewModel
{
    [StringLength(20, ErrorMessage = "The icon cannot exceed 20 characters.")]
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
    public string? Icon { get; set; }

    [Required(ErrorMessage = "The English topic name is required.")]
    [StringLength(50, ErrorMessage = "The English topic name cannot exceed 50 characters.")]
    public string? TopicName { get; set; }

    [StringLength(50, ErrorMessage = "The Vietnamese topic name cannot exceed 50 characters.")]
    public string? TopicNameVi { get; set; }

    public List<string> Keywords { get; set; } = new();

    public List<uint> WordIds { get; set; } = new();
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
}

public sealed class TopicDetailViewModel
{
    public required AdminTopic Topic { get; init; }
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
}
