using VocaNova.Dashboard.Models.Api.Knn;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.Knn;

/// <summary>
/// Mô tả 1 lookup KNN — quyết định cột bảng, field form, và quy tắc validate.
/// <see cref="Key"/> dùng chung cho route dashboard (`/knn/{key}`) và path API (`api/admin/knn/{key}`).
/// </summary>
public sealed class KnnTypeDescriptor
{
    public required string Key { get; init; }

    /// <summary>Resource key cho tên số nhiều của lookup (dùng ở nav + tiêu đề).</summary>
    public required string TitleKey { get; init; }

    /// <summary>Resource key cho tên số ít (dùng ở nút "New …").</summary>
    public required string SingularKey { get; init; }

    public bool HasCode { get; init; }

    public bool HasParent { get; init; }

    public bool HasAge { get; init; }

    public bool HasDisplayOrder { get; init; }

    public bool HasDescription { get; init; }

    public int NameMaxLength { get; init; } = 100;
}

public static class KnnTypes
{
    public static readonly IReadOnlyList<KnnTypeDescriptor> All = new[]
    {
        new KnnTypeDescriptor { Key = "age-ranges", TitleKey = "Knn.AgeRanges", SingularKey = "Knn.AgeRange", HasAge = true, HasDisplayOrder = true, NameMaxLength = 50 },
        new KnnTypeDescriptor { Key = "regions", TitleKey = "Knn.Regions", SingularKey = "Knn.Region", HasCode = true, HasParent = true, NameMaxLength = 100 },
        new KnnTypeDescriptor { Key = "occupations", TitleKey = "Knn.Occupations", SingularKey = "Knn.Occupation", HasDescription = true, NameMaxLength = 100 },
        new KnnTypeDescriptor { Key = "education-levels", TitleKey = "Knn.EducationLevels", SingularKey = "Knn.EducationLevel", HasDescription = true, HasDisplayOrder = true, NameMaxLength = 100 },
        new KnnTypeDescriptor { Key = "learning-purposes", TitleKey = "Knn.LearningPurposes", SingularKey = "Knn.LearningPurpose", HasDescription = true, NameMaxLength = 100 },
    };

    public static KnnTypeDescriptor? Find(string? key)
        => All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
}

public sealed class KnnListQuery
{
    public string? Q { get; set; }

    public bool IncludeDeleted { get; set; }

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 20;
}

public sealed class KnnListViewModel
{
    public required KnnTypeDescriptor Type { get; init; }

    public KnnListQuery Query { get; init; } = new();

    public IReadOnlyList<KnnItemDto> Items { get; init; } = Array.Empty<KnnItemDto>();

    public bool Loaded { get; init; }

    public PaginationInfo? Pagination { get; init; }
}

public sealed class KnnFormViewModel
{
    public required KnnTypeDescriptor Type { get; set; }

    public uint? Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public uint? ParentId { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public int DisplayOrder { get; set; }

    public string? Description { get; set; }

    public string? Error { get; set; }

    /// <summary>Danh sách region active để chọn parent (chỉ dùng cho type regions).</summary>
    public IReadOnlyList<KnnItemDto> ParentOptions { get; set; } = Array.Empty<KnnItemDto>();

    public bool IsEdit => Id.HasValue;
}

public sealed class KnnOverviewViewModel
{
    public KnnConfigDto? Config { get; init; }

    public bool ConfigLoaded { get; init; }

    public KnnRebuildStatusDto? RebuildStatus { get; init; }

    public bool StatusLoaded { get; init; }
}
