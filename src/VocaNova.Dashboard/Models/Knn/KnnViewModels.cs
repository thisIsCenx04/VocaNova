using VocaNova.Dashboard.Data.Dtos.Knn;

namespace VocaNova.Dashboard.Models.Knn;

/// <summary>Một trường trong form create/edit lookup.</summary>
public sealed record KnnFieldDef(string Name, string Label, string Type, string? Value = null, bool Required = false);

/// <summary>Một dòng lookup: ô hiển thị + giá trị cho form sửa inline.</summary>
public sealed record KnnLookupRow(uint Id, string Status, IReadOnlyList<string> Cells, IReadOnlyList<KnnFieldDef> Fields);

/// <summary>Một cột của bảng lookup. <c>SortKey</c> null = cột không sort được.</summary>
public sealed record KnnColumn(string Label, string? SortKey = null);

/// <summary>Trang quản lý 1 lookup KNN (dùng chung cho cả 5 loại).</summary>
public sealed class KnnLookupViewModel
{
    public required string Slug { get; init; }

    public required string Title { get; init; }

    public IReadOnlyList<KnnColumn> Columns { get; init; } = Array.Empty<KnnColumn>();

    /// <summary>Cột đang sort, dùng khoá sort của API (id | name | status | min_age | ...).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

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

/// <summary>Một ô trọng số trong form chỉnh vector KNN.</summary>
public sealed record KnnWeightField(
    string Name,
    string Label,
    double Value,
    double Default,
    string Source);

public sealed class KnnOverviewViewModel
{
    public KnnConfig? Config { get; init; }

    public KnnRebuildStatus? RebuildStatus { get; init; }

    public IReadOnlyList<KnnLookupLink> Lookups { get; init; } = Array.Empty<KnnLookupLink>();

    public KnnVectorConfig? Vector => Config?.Vector;

    /// <summary>
    /// Bốn trường đầu thu ở form đăng ký, hai trường cuối ở bộ câu hỏi onboarding.
    /// </summary>
    public IReadOnlyList<KnnWeightField> WeightFields
    {
        get
        {
            var vector = Vector;
            if (vector is null)
            {
                return Array.Empty<KnnWeightField>();
            }

            return
            [
                new("age_range_weight", "Age range", vector.Weights.AgeRangeWeight, vector.Defaults.AgeRangeWeight, "Sign-up"),
                new("region_weight", "Region", vector.Weights.RegionWeight, vector.Defaults.RegionWeight, "Sign-up"),
                new("occupation_weight", "Occupation", vector.Weights.OccupationWeight, vector.Defaults.OccupationWeight, "Sign-up"),
                new("education_level_weight", "Education level", vector.Weights.EducationLevelWeight, vector.Defaults.EducationLevelWeight, "Sign-up"),
                new("learning_purpose_weight", "Learning purpose", vector.Weights.LearningPurposeWeight, vector.Defaults.LearningPurposeWeight, "Onboarding"),
                new("interest_topics_weight", "Interest topics", vector.Weights.InterestTopicsWeight, vector.Defaults.InterestTopicsWeight, "Onboarding"),
            ];
        }
    }
}
