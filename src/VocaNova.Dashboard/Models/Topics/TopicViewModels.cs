using VocaNova.Dashboard.Models.Api.Dictionary;

namespace VocaNova.Dashboard.Models.Topics;

public sealed class TopicListViewModel
{
    public IReadOnlyList<TopicSummaryDto> Topics { get; init; } = Array.Empty<TopicSummaryDto>();

    public bool Loaded { get; init; }

    /// <summary>
    /// G6: chưa có admin topic list (includeDeleted) nên dashboard không liệt kê được topic đã xóa.
    /// Khi API có, bật cờ này để hiện tab/toggle "đã xóa" + nút Restore.
    /// </summary>
    public bool RestoreAvailable { get; init; }
}

public sealed class TopicFormViewModel
{
    public uint? TopicId { get; set; }

    public string? TopicName { get; set; }

    public string? TopicNameVi { get; set; }

    public string? Icon { get; set; }

    /// <summary>Thông báo lỗi (validation client hoặc message từ API) để render lại trong modal.</summary>
    public string? Error { get; set; }

    public bool IsEdit => TopicId.HasValue;
}
