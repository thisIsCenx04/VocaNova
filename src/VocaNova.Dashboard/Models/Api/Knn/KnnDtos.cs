namespace VocaNova.Dashboard.Models.Api.Knn;

// Map từ envelope API qua ApiJson.Default (SnakeCaseLower) — không field nào chứa số nên không cần [JsonPropertyName].

/// <summary>
/// DTO hợp nhất cho cả 5 lookup KNN. Mỗi type chỉ dùng tập field liên quan;
/// id riêng từng bảng được gộp về <see cref="Id"/>.
/// </summary>
public sealed class KnnItemDto
{
    public uint? AgeRangeId { get; set; }

    public uint? RegionId { get; set; }

    public uint? OccupationId { get; set; }

    public uint? EducationLevelId { get; set; }

    public uint? LearningPurposeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public uint? ParentId { get; set; }

    public string? ParentName { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public int DisplayOrder { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public uint Id => AgeRangeId ?? RegionId ?? OccupationId ?? EducationLevelId ?? LearningPurposeId ?? 0;
}

public sealed class KnnConfigDto
{
    public KnnOnboardingConfigDto? Onboarding { get; set; }

    public KnnLearningConfigDto? Learning { get; set; }
}

public sealed class KnnOnboardingConfigDto
{
    public int KValue { get; set; }

    public int DefaultTopicLimit { get; set; }

    public double MinSimilarity { get; set; }

    public int CacheTtlMinutes { get; set; }
}

public sealed class KnnLearningConfigDto
{
    public int KValue { get; set; }

    public int MinSessions { get; set; }

    public double MinSimilarity { get; set; }

    public int RecommendationCount { get; set; }

    public int RebuildIntervalHours { get; set; }

    public int CacheTtlMinutes { get; set; }
}

public sealed class KnnRebuildStatusDto
{
    public DateTime? LastRebuildAt { get; set; }

    public bool IsRunning { get; set; }
}
