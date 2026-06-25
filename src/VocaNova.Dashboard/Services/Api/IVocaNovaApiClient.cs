using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Models.Api.Stats;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Client gọi các endpoint admin của VocaNova.API. Mọi request tự đính kèm Bearer token + refresh qua <see cref="BearerTokenHandler"/>.
/// </summary>
public interface IVocaNovaApiClient
{
    // F056 — Overview stats.
    Task<DashboardStats?> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<SessionsTrend?> GetSessionsTrendAsync(int days, CancellationToken cancellationToken = default);

    Task<MasteryDistribution?> GetMasteryDistributionAsync(CancellationToken cancellationToken = default);

    // F057 — Vocabulary list & filter.
    Task<PagedData<WordListItem>> GetWordsAsync(WordListFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopicSummary>> GetTopicsAsync(CancellationToken cancellationToken = default);

    Task<ApiActionResult> DeleteWordAsync(uint wordId, CancellationToken cancellationToken = default);

    Task<ApiActionResult> RestoreWordAsync(uint wordId, CancellationToken cancellationToken = default);
}

/// <summary>Tham số lọc danh sách từ vựng gửi tới API (F057).</summary>
public sealed record WordListFilter(
    string? Q,
    string? Cefr,
    uint? TopicId,
    string? Status,
    bool IncludeDeleted,
    int Page,
    int Limit);
