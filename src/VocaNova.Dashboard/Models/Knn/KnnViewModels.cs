using VocaNova.Dashboard.Models.Api.Knn;

namespace VocaNova.Dashboard.Models.Knn;

/// <summary>Một trường trong form create/edit lookup.</summary>
public sealed record KnnFieldDef(string Name, string Label, string Type, string? Value = null, bool Required = false);

/// <summary>Một dòng lookup: ô hiển thị + giá trị cho form sửa inline.</summary>
public sealed record KnnLookupRow(uint Id, string Status, IReadOnlyList<string> Cells, IReadOnlyList<KnnFieldDef> Fields);

/// <summary>Trang quản lý 1 lookup KNN (dùng chung cho cả 5 loại).</summary>
public sealed class KnnLookupViewModel
{
    public required string Slug { get; init; }

    public required string Title { get; init; }

    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<KnnFieldDef> AddFields { get; init; } = Array.Empty<KnnFieldDef>();

    public IReadOnlyList<KnnLookupRow> Rows { get; init; } = Array.Empty<KnnLookupRow>();

    public string? Q { get; init; }

    public string? Status { get; init; }

    public bool IncludeDeleted { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}

public sealed record KnnLookupLink(string Slug, string Title, string Description);

public sealed class KnnOverviewViewModel
{
    public KnnConfig? Config { get; init; }

    public KnnRebuildStatus? RebuildStatus { get; init; }

    public IReadOnlyList<KnnLookupLink> Lookups { get; init; } = Array.Empty<KnnLookupLink>();
}
