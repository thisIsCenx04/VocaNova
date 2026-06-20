namespace VocaNova.Dashboard.Models.Components;

public enum StatDelta
{
    None,
    Up,
    Down,
}

public sealed class StatCardViewModel
{
    public string Eyebrow { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string? Delta { get; init; }

    public StatDelta Direction { get; init; } = StatDelta.None;
}

/// <summary>Badge tròn. <see cref="Modifier"/> ∈ ok|warn|err|info|a1..c2 (map class <c>.badge--*</c>).</summary>
public sealed class BadgeViewModel
{
    public string Text { get; init; } = string.Empty;

    public string Modifier { get; init; } = "info";
}

public enum BlockState
{
    Loading,
    Empty,
    Error,
}

public sealed class StateBlockViewModel
{
    public BlockState State { get; init; }

    public string? Title { get; init; }

    public string? Message { get; init; }

    public string? ActionText { get; init; }

    public string? ActionUrl { get; init; }

    public int SkeletonRows { get; init; } = 4;
}

public sealed class PaginationViewModel
{
    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalItems { get; init; }

    /// <summary>Hàm dựng URL cho 1 trang (caller cung cấp, giữ nguyên filter hiện tại).</summary>
    public Func<int, string> PageUrl { get; init; } = _ => "#";
}

public sealed class ChartCardViewModel
{
    public string Title { get; init; } = string.Empty;

    public string CanvasId { get; init; } = "chart";

    /// <summary>Hiện dropdown granularity (daily/weekly/monthly) — F062.</summary>
    public bool ShowGranularity { get; init; }
}

/// <summary>Helper map status/CEFR → modifier class cho <see cref="BadgeViewModel"/>.</summary>
public static class BadgeModifiers
{
    public static string Status(string? status) => status switch
    {
        "active" => "ok",
        "locked" => "warn",
        "deleted" => "err",
        _ => "info",
    };

    public static string Cefr(string? cefr) => (cefr?.Trim().ToLowerInvariant()) switch
    {
        "a1" => "a1",
        "a2" => "a2",
        "b1" => "b1",
        "b2" => "b2",
        "c1" => "c1",
        "c2" => "c2",
        _ => "info",
    };
}
