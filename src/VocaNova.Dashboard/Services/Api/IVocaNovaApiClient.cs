using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Models.Api.Stats;
using VocaNova.Dashboard.Models.Api.Users;

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

    // F058 — Vocabulary detail & sense management.
    Task<WordDetail?> GetWordDetailAsync(uint wordId, CancellationToken cancellationToken = default);

    Task<ApiActionResult> CreateSenseAsync(uint wordId, SenseInput input, CancellationToken cancellationToken = default);

    Task<ApiActionResult> UpdateSenseAsync(uint wordId, uint senseId, SenseInput input, CancellationToken cancellationToken = default);

    Task<ApiActionResult> UploadAudioAsync(uint wordId, AudioUpload upload, CancellationToken cancellationToken = default);

    Task<ApiActionResult> DeleteAudioAsync(uint wordId, uint audioId, CancellationToken cancellationToken = default);

    Task<ApiActionResult> UploadImageAsync(uint wordId, ImageUpload upload, CancellationToken cancellationToken = default);

    // F059 — CSV import. Trả về kết quả import (imported/skipped/errors) hoặc lỗi.
    Task<ImportWordsResult> ImportWordsAsync(FileUpload upload, CancellationToken cancellationToken = default);

    // F060 — User management.
    Task<PagedData<AdminUserSummary>> GetUsersAsync(UserListFilter filter, CancellationToken cancellationToken = default);

    Task<AdminUserDetail?> GetUserDetailAsync(uint userId, CancellationToken cancellationToken = default);

    Task<PagedData<AdminUserTestSession>> GetUserTestHistoryAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default);

    Task<PagedData<AuditLog>> GetUserAuditLogsAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default);

    Task<ApiActionResult> DeactivateUserAsync(uint userId, CancellationToken cancellationToken = default);

    Task<ApiActionResult> RestoreUserAsync(uint userId, CancellationToken cancellationToken = default);

    // F061 — Topic management.
    Task<IReadOnlyList<AdminTopic>> GetAdminTopicsAsync(string? q, string? status, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<ApiActionResult> CreateTopicAsync(TopicInput input, CancellationToken cancellationToken = default);

    Task<ApiActionResult> UpdateTopicAsync(uint topicId, TopicInput input, CancellationToken cancellationToken = default);

    Task<ApiActionResult> DeleteTopicAsync(uint topicId, CancellationToken cancellationToken = default);

    Task<ApiActionResult> RestoreTopicAsync(uint topicId, CancellationToken cancellationToken = default);
}

/// <summary>Payload tạo/cập nhật topic (khớp CreateTopicRequest/UpdateTopicRequest của API).</summary>
public sealed record TopicInput(string? TopicName, string? TopicNameVi, string? Icon);

/// <summary>Bộ lọc danh sách user (F060).</summary>
public sealed record UserListFilter(string? Status, string? Search, bool IncludeDeleted, int Page, int Limit);

/// <summary>Payload tạo/cập nhật sense (khớp CreateSenseRequest/UpdateSenseRequest của API).</summary>
public sealed record SenseInput(int SenseOrder, string? WordClass, string? EnglishDefinition, string? VietnameseMeaning);

public sealed record AudioUpload(string Accent, Stream Content, string FileName, string ContentType);

public sealed record ImageUpload(Stream Content, string FileName, string ContentType);

public sealed record FileUpload(Stream Content, string FileName, string ContentType);

/// <summary>Kết quả import CSV: thành công kèm dữ liệu, hoặc lỗi kèm message.</summary>
public sealed record ImportWordsResult(bool IsSuccess, int StatusCode, string? Message, Models.Api.Dictionary.BulkImportResult? Data)
{
    public static ImportWordsResult Ok(Models.Api.Dictionary.BulkImportResult data) => new(true, 200, null, data);

    public static ImportWordsResult Fail(int statusCode, string? message) => new(false, statusCode, message, null);
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
