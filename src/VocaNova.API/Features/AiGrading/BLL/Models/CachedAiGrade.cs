namespace VocaNova.API.Features.AiGrading.BLL.Models;

public sealed record CachedAiGrade(
    float Score,
    string? Explanation,
    string? Suggestion,
    AiGradeCacheKey? Key = null,
    int HitCount = 1,
    DateTime? CreatedAt = null,
    DateTime? ExpiresAt = null);
