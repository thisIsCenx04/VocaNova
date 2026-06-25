using VocaNova.Dashboard.Models.Api.Dictionary;

namespace VocaNova.Dashboard.Models.Topics;

public sealed class TopicListViewModel
{
    public IReadOnlyList<AdminTopic> Items { get; init; } = Array.Empty<AdminTopic>();

    public string? Q { get; init; }

    public string? Status { get; init; }

    public bool IncludeDeleted { get; init; }
}
