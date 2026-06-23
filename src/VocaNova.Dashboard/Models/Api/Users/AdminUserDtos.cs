namespace VocaNova.Dashboard.Models.Api.Users;

// Map từ envelope API qua ApiJson.Default (SnakeCaseLower) — không field nào chứa số nên không cần [JsonPropertyName].

public sealed class AdminUserSummaryDto
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class AdminUserDetailDto
{
    public uint UserId { get; set; }

    public string? Phone { get; set; }

    public string? GoogleEmail { get; set; }

    public string? Username { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public AdminUserLearningProfileDto? LearningProfile { get; set; }
}

public sealed class AdminUserLearningProfileDto
{
    public uint? AgeRangeId { get; set; }

    public string? AgeRangeName { get; set; }

    public uint? RegionId { get; set; }

    public string? RegionName { get; set; }

    public uint? OccupationId { get; set; }

    public string? OccupationName { get; set; }

    public uint? EducationLevelId { get; set; }

    public string? EducationLevelName { get; set; }

    public uint? LearningPurposeId { get; set; }

    public string? LearningPurposeName { get; set; }
}
