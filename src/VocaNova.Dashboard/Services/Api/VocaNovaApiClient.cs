using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VocaNova.Dashboard.Data.Dtos.Dictionary;
using VocaNova.Dashboard.Data.Dtos.Stats;

namespace VocaNova.Dashboard.Services.Api;

public sealed class VocaNovaApiClient : IVocaNovaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VocaNovaApiClient> _logger;

    public VocaNovaApiClient(HttpClient httpClient, ILogger<VocaNovaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<DashboardStats?> GetDashboardStatsAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<DashboardStats>("api/admin/stats/dashboard", cancellationToken);

    public Task<SessionsTrend?> GetSessionsTrendAsync(int days, CancellationToken cancellationToken = default) =>
        GetDataAsync<SessionsTrend>($"api/admin/stats/sessions-trend?days={days}", cancellationToken);

    public Task<MasteryDistribution?> GetMasteryDistributionAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<MasteryDistribution>("api/admin/stats/mastery-distribution", cancellationToken);

    public Task<ActivityTrend?> GetActivityTrendAsync(string granularity, CancellationToken cancellationToken = default) =>
        GetDataAsync<ActivityTrend>($"api/admin/stats/activity-trend?granularity={Uri.EscapeDataString(granularity)}", cancellationToken);

    public Task<LearningStats?> GetLearningStatsAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<LearningStats>("api/admin/stats/learning", cancellationToken);

    public Task<Demographics?> GetDemographicsAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<Demographics>("api/admin/stats/demographics", cancellationToken);

    public Task<IReadOnlyList<TopicSummary>> GetTopicsAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<TopicSummary>("api/topics", cancellationToken);

    public Task<PagedData<WordListItem>> GetWordsAsync(WordListFilter filter, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = filter.Limit.ToString(CultureInfo.InvariantCulture),
            ["includeDeleted"] = filter.IncludeDeleted ? "true" : "false",
        };

        if (!string.IsNullOrWhiteSpace(filter.Q)) queryParams["q"] = filter.Q;
        if (!string.IsNullOrWhiteSpace(filter.Cefr)) queryParams["cefr"] = filter.Cefr;
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams["status"] = filter.Status;
        if (!string.IsNullOrWhiteSpace(filter.WordType)) queryParams["wordType"] = filter.WordType;
        if (filter.TopicId is { } topicId) queryParams["topicId"] = topicId.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(filter.SortBy)) queryParams["sortBy"] = filter.SortBy;
        if (!string.IsNullOrWhiteSpace(filter.SortDirection)) queryParams["sortDirection"] = filter.SortDirection;

        var uri = QueryHelpers.AddQueryString("api/admin/words", queryParams);
        return GetPagedAsync<WordListItem>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<ApiActionResult> DeleteWordAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/words/{wordId}", cancellationToken);

    public Task<ApiActionResult> RestoreWordAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/words/{wordId}/restore", cancellationToken);

    public Task<ApiActionResult> CreateWordAsync(WordInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, "api/admin/words", new
        {
            input.Word,
            input.Cefr,
            input.PhoneticUk,
            input.PhoneticUs,
            input.ImageUrl,
            input.IsPhrase,
        }, cancellationToken);

    public async Task<(ApiActionResult Result, uint? WordId)> CreateWordWithIdAsync(WordInput input, CancellationToken cancellationToken = default)
    {
        const string requestUri = "api/admin/words";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(
                    new { input.Word, input.Cefr, input.PhoneticUk, input.PhoneticUs, input.IsPhrase },
                    options: ApiJson.Default),
            };
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                uint? newId = null;
                try
                {
                    var envelope = await response.Content
                        .ReadFromJsonAsync<ApiEnvelope<CreatedWordRef>>(ApiJson.Default, cancellationToken);
                    newId = envelope?.Data?.WordId;
                }
                catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
                {
                    // Không parse được id → vẫn coi là tạo thành công, chỉ là không thêm sense được.
                }

                return (ApiActionResult.Ok(statusCode), newId);
            }

            string? message = null;
            try
            {
                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<object>>(ApiJson.Default, cancellationToken);
                message = envelope?.Message ?? envelope?.Errors.FirstOrDefault();
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
            {
                // body lỗi không parse được → message null.
            }

            LogNonSuccess(requestUri, statusCode);
            return (ApiActionResult.Fail(statusCode, message), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "VocaNova.API POST {RequestUri} failed.", requestUri);
            return (ApiActionResult.Fail(0, null), null);
        }
    }

    public Task<ApiActionResult> UpdateWordAsync(uint wordId, WordInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/words/{wordId}", new
        {
            input.Word,
            input.Cefr,
            input.PhoneticUk,
            input.PhoneticUs,
            input.ImageUrl,
            input.IsPhrase,
        }, cancellationToken);

    public Task<WordDetail?> GetWordDetailAsync(uint wordId, CancellationToken cancellationToken = default) =>
        GetDataAsync<WordDetail>($"api/words/{wordId}", cancellationToken);

    public Task<ApiActionResult> CreateSenseAsync(uint wordId, SenseInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, $"api/admin/words/{wordId}/senses", SensePayload(input), cancellationToken);

    public Task<ApiActionResult> UpdateSenseAsync(uint wordId, uint senseId, SenseInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/words/{wordId}/senses/{senseId}", SensePayload(input), cancellationToken);

    public Task<ApiActionResult> DeleteAudioAsync(uint wordId, uint audioId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/words/{wordId}/audio/{audioId}", cancellationToken);

    public Task<ApiActionResult> UploadAudioAsync(uint wordId, AudioUpload upload, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(upload.Accent), "accent" },
        };
        var file = new StreamContent(upload.Content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
        content.Add(file, "file", upload.FileName);

        return SendMultipartActionAsync(HttpMethod.Post, $"api/admin/words/{wordId}/audio", content, cancellationToken);
    }

    public Task<ApiActionResult> UploadImageAsync(uint wordId, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        var file = new StreamContent(upload.Content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
        content.Add(file, "file", upload.FileName);

        return SendMultipartActionAsync(HttpMethod.Post, $"api/admin/words/{wordId}/image", content, cancellationToken);
    }

    public Task<ApiActionResult> UpdateImageUrlAsync(uint wordId, string? imageUrl, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/words/{wordId}/image", new { image_url = imageUrl }, cancellationToken);

    public async Task<ImportWordsResult> ImportWordsAsync(FileUpload upload, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var file = new StreamContent(upload.Content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
            content.Add(file, "file", upload.FileName);

            using var response = await _httpClient.PostAsync("api/admin/words/import", content, cancellationToken);
            var statusCode = (int)response.StatusCode;

            var envelope = await response.Content
                .ReadFromJsonAsync<ApiEnvelope<VocaNova.Dashboard.Data.Dtos.Dictionary.BulkImportResult>>(ApiJson.Default, cancellationToken);

            if (response.IsSuccessStatusCode && envelope?.Data is not null)
            {
                return ImportWordsResult.Ok(envelope.Data);
            }

            LogNonSuccess("api/admin/words/import", statusCode);
            return ImportWordsResult.Fail(statusCode, envelope?.Message ?? envelope?.Errors.FirstOrDefault());
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "VocaNova.API import words failed.");
            return ImportWordsResult.Fail(0, null);
        }
    }

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.Users.AdminUserSummary>> GetUsersAsync(UserListFilter filter, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = filter.Limit.ToString(CultureInfo.InvariantCulture),
            ["includeDeleted"] = filter.IncludeDeleted ? "true" : "false",
        };
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams["status"] = filter.Status;
        if (!string.IsNullOrWhiteSpace(filter.Search)) queryParams["search"] = filter.Search;
        if (!string.IsNullOrWhiteSpace(filter.Role)) queryParams["role"] = filter.Role;
        if (!string.IsNullOrWhiteSpace(filter.SortBy)) queryParams["sortBy"] = filter.SortBy;
        if (!string.IsNullOrWhiteSpace(filter.SortDirection)) queryParams["sortDirection"] = filter.SortDirection;

        var uri = QueryHelpers.AddQueryString("api/admin/users", queryParams);
        return GetPagedAsync<VocaNova.Dashboard.Data.Dtos.Users.AdminUserSummary>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<VocaNova.Dashboard.Data.Dtos.Users.AdminUserTopics?> GetUserTopicsAsync(uint userId, CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Users.AdminUserTopics>($"api/admin/users/{userId}/topics", cancellationToken);

    public Task<VocaNova.Dashboard.Data.Dtos.Users.AdminUserDetail?> GetUserDetailAsync(uint userId, CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Users.AdminUserDetail>($"api/admin/users/{userId}", cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.Users.AdminUserTestSession>> GetUserTestHistoryAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default) =>
        GetPagedAsync<VocaNova.Dashboard.Data.Dtos.Users.AdminUserTestSession>(
            $"api/admin/users/{userId}/test-history?page={page}&limit={limit}", page, limit, cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.Users.AuditLog>> GetUserAuditLogsAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default) =>
        GetPagedAsync<VocaNova.Dashboard.Data.Dtos.Users.AuditLog>(
            $"api/admin/audit-logs?userId={userId}&page={page}&limit={limit}", page, limit, cancellationToken);

    public Task<ApiActionResult> DeactivateUserAsync(uint userId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/users/{userId}/deactivate", cancellationToken);

    public Task<ApiActionResult> RestoreUserAsync(uint userId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/users/{userId}/restore", cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.SuperAdmin.AdminAccount>> GetAdminAccountsAsync(
        AdminAccountFilter filter,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = filter.Limit.ToString(CultureInfo.InvariantCulture),
            ["includeDeleted"] = filter.IncludeDeleted ? "true" : "false",
        };
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams["status"] = filter.Status;
        if (!string.IsNullOrWhiteSpace(filter.Search)) queryParams["search"] = filter.Search;
        if (!string.IsNullOrWhiteSpace(filter.SortBy)) queryParams["sort_by"] = filter.SortBy;
        if (!string.IsNullOrWhiteSpace(filter.SortDirection)) queryParams["sort_direction"] = filter.SortDirection;

        var uri = QueryHelpers.AddQueryString("api/superadmin/admins", queryParams);
        return GetPagedAsync<VocaNova.Dashboard.Data.Dtos.SuperAdmin.AdminAccount>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<VocaNova.Dashboard.Data.Dtos.SuperAdmin.AdminAccount?> GetAdminAccountAsync(
        uint adminId,
        CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.SuperAdmin.AdminAccount>($"api/superadmin/admins/{adminId}", cancellationToken);

    public Task<ApiActionResult> CreateAdminAccountAsync(
        AdminAccountInput input,
        CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, "api/superadmin/admins", AdminAccountPayload(input), cancellationToken);

    public Task<ApiActionResult> UpdateAdminAccountAsync(
        uint adminId,
        AdminAccountInput input,
        CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/superadmin/admins/{adminId}", AdminAccountPayload(input), cancellationToken);

    public Task<ApiActionResult> LockAdminAccountAsync(uint adminId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/superadmin/admins/{adminId}/lock", cancellationToken);

    public Task<ApiActionResult> UnlockAdminAccountAsync(uint adminId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/superadmin/admins/{adminId}/unlock", cancellationToken);

    public Task<ApiActionResult> DeleteAdminAccountAsync(uint adminId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/superadmin/admins/{adminId}", cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.SuperAdmin.ManagedRole>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        GetRolesAsync(null, null, cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.SuperAdmin.ManagedRole>> GetRolesAsync(
        string? search, string? type, CancellationToken cancellationToken = default) =>
        GetRolesAsync(search, type, null, null, cancellationToken);

    public Task<PagedData<VocaNova.Dashboard.Data.Dtos.SuperAdmin.ManagedRole>> GetRolesAsync(
        string? search,
        string? type,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?> { ["page"] = "1", ["limit"] = "100" };
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;
        if (!string.IsNullOrWhiteSpace(sortBy)) query["sort_by"] = sortBy;
        if (!string.IsNullOrWhiteSpace(sortDirection)) query["sort_direction"] = sortDirection;
        return GetPagedAsync<VocaNova.Dashboard.Data.Dtos.SuperAdmin.ManagedRole>(
            QueryHelpers.AddQueryString("api/superadmin/roles", query), 1, 100, cancellationToken);
    }

    public Task<ApiActionResult> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, "api/superadmin/roles", new { RoleName = roleName }, cancellationToken);

    public Task<ApiActionResult> UpdateRoleAsync(uint roleId, string roleName, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/superadmin/roles/{roleId}", new { RoleName = roleName }, cancellationToken);

    public Task<ApiActionResult> DeleteRoleAsync(uint roleId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/superadmin/roles/{roleId}", cancellationToken);

    public Task<ApiActionResult> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Post, $"api/superadmin/roles/{roleId}/users/{userId}", cancellationToken);

    public Task<IReadOnlyList<VocaNova.Dashboard.Data.Dtos.Dictionary.AdminTopic>> GetAdminTopicsAsync(
        string? q,
        string? status,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["includeDeleted"] = includeDeleted ? "true" : "false",
        };
        if (!string.IsNullOrWhiteSpace(q)) queryParams["q"] = q;
        if (!string.IsNullOrWhiteSpace(status)) queryParams["status"] = status;

        var uri = QueryHelpers.AddQueryString("api/admin/topics", queryParams);
        return GetListAsync<VocaNova.Dashboard.Data.Dtos.Dictionary.AdminTopic>(uri, cancellationToken);
    }

    public Task<ApiActionResult> CreateTopicAsync(TopicInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, "api/admin/topics", TopicPayload(input), cancellationToken);

    public Task<ApiActionResult> AddWordsToTopicAsync(uint topicId, IReadOnlyCollection<uint> wordIds, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, $"api/admin/topics/{topicId}/words", new { WordIds = wordIds }, cancellationToken);

    public Task<ApiActionResult> UpdateTopicAsync(uint topicId, TopicInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/topics/{topicId}", TopicPayload(input), cancellationToken);

    public Task<ApiActionResult> DeleteTopicAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/topics/{topicId}", cancellationToken);

    public Task<ApiActionResult> RestoreTopicAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/topics/{topicId}/restore", cancellationToken);

    public Task<VocaNova.Dashboard.Data.Dtos.Knn.KnnConfig?> GetKnnConfigAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Knn.KnnConfig>("api/admin/knn/config", cancellationToken);

    public Task<VocaNova.Dashboard.Data.Dtos.Knn.KnnRebuildStatus?> GetKnnRebuildStatusAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Knn.KnnRebuildStatus>("api/admin/knn/rebuild-status", cancellationToken);

    public Task<ApiActionResult> TriggerKnnRebuildAsync(CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Post, "api/admin/knn/trigger-rebuild", cancellationToken);

    public Task<PagedData<T>> GetKnnLookupPageAsync<T>(string lookup, KnnLookupFilter filter, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = filter.Limit.ToString(CultureInfo.InvariantCulture),
            ["include_deleted"] = filter.IncludeDeleted ? "true" : "false",
        };
        if (!string.IsNullOrWhiteSpace(filter.Q)) queryParams["q"] = filter.Q;
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams["status"] = filter.Status;
        if (!string.IsNullOrWhiteSpace(filter.SortBy)) queryParams["sort_by"] = filter.SortBy;
        if (!string.IsNullOrWhiteSpace(filter.SortDirection)) queryParams["sort_direction"] = filter.SortDirection;

        var uri = QueryHelpers.AddQueryString($"api/admin/knn/{lookup}", queryParams);
        return GetPagedAsync<T>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<ApiActionResult> CreateKnnLookupAsync(string lookup, object payload, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, $"api/admin/knn/{lookup}", payload, cancellationToken);

    public Task<ApiActionResult> UpdateKnnLookupAsync(string lookup, uint id, object payload, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/knn/{lookup}/{id}", payload, cancellationToken);

    public Task<ApiActionResult> DeleteKnnLookupAsync(string lookup, uint id, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/knn/{lookup}/{id}", cancellationToken);

    public Task<ApiActionResult> RestoreKnnLookupAsync(string lookup, uint id, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/knn/{lookup}/{id}/restore", cancellationToken);

    public Task<ApiActionResult> UpdateKnnVectorWeightsAsync(
        VocaNova.Dashboard.Data.Dtos.Knn.KnnVectorWeights weights,
        CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, "api/admin/knn/config/vector-weights", new
        {
            weights.AgeRangeWeight,
            weights.RegionWeight,
            weights.OccupationWeight,
            weights.EducationLevelWeight,
            weights.LearningPurposeWeight,
            weights.InterestTopicsWeight,
        }, cancellationToken);

    public Task<ApiActionResult> ResetKnnVectorWeightsAsync(CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Post, "api/admin/knn/config/vector-weights/reset", cancellationToken);

    public Task<VocaNova.Dashboard.Data.Dtos.Settings.AiGradingConfig?> GetAiGradingConfigAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Settings.AiGradingConfig>("api/admin/settings/ai-grading", cancellationToken);

    public Task<ApiActionResult> UpdateAiGradingConfigAsync(
        VocaNova.Dashboard.Data.Dtos.Settings.AiGradingConfigInput input,
        CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, "api/admin/settings/ai-grading", new
        {
            input.Provider,
            input.Endpoint,
            input.Model,
            input.FallbackModels,
            input.ApiKey,
            input.MaxAttempts,
            input.RetryBaseDelayMs,
            input.AttemptTimeoutSeconds,
            input.PassThreshold,
        }, cancellationToken);

    public Task<ApiActionResult> ResetAiGradingConfigAsync(CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Post, "api/admin/settings/ai-grading/reset", cancellationToken);

    public async Task<VocaNova.Dashboard.Data.Dtos.Settings.AiGradingConnectionTest?> TestAiGradingConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        const string requestUri = "api/admin/settings/ai-grading/test";
        try
        {
            using var response = await _httpClient.PostAsync(requestUri, content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogNonSuccess(requestUri, (int)response.StatusCode);
                return null;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<ApiEnvelope<VocaNova.Dashboard.Data.Dtos.Settings.AiGradingConnectionTest>>(
                    ApiJson.Default,
                    cancellationToken);
            return envelope?.Data;
        }
        catch (Exception ex)
            when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "VocaNova.API POST {RequestUri} failed.", requestUri);
            return null;
        }
    }

    public Task<VocaNova.Dashboard.Data.Dtos.Auth.MeProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<VocaNova.Dashboard.Data.Dtos.Auth.MeProfile>("api/auth/me", cancellationToken);

    public Task<ApiActionResult> UpdateMyProfileAsync(string? displayName, string? avatarUrl, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, "api/auth/me/profile", new
        {
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
        }, cancellationToken);

    public Task<ApiActionResult> ChangeMyPasswordAsync(string? currentPassword, string? newPassword, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, "api/auth/me/password", new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
        }, cancellationToken);

    public Task<ApiActionResult> UploadMyAvatarAsync(ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        var file = new StreamContent(upload.Content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
        content.Add(file, "file", upload.FileName);

        return SendMultipartActionAsync(HttpMethod.Post, "api/auth/me/avatar", content, cancellationToken);
    }

    private static object TopicPayload(TopicInput input) => new
    {
        input.TopicName,
        input.TopicNameVi,
        input.Icon,
        input.WordIds,
    };

    private static object AdminAccountPayload(AdminAccountInput input) => new
    {
        input.FullName,
        input.Email,
        input.Phone,
        input.Password,
        input.Status,
    };

    // Chỉ cần word_id từ response tạo word (POST /api/admin/words trả về word DTO đầy đủ).
    private sealed class CreatedWordRef
    {
        public uint WordId { get; set; }
    }

    private static object SensePayload(SenseInput input) => new
    {
        input.SenseOrder,
        input.WordClass,
        input.EnglishDefinition,
        input.VietnameseMeaning,
        Examples = input.Examples?
            .Select(example => new { example.ExampleId, example.ExampleEn, example.ExampleVi })
            .ToArray(),
    };

    private async Task<T?> GetDataAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogNonSuccess(requestUri, (int)response.StatusCode);
                return default;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<ApiEnvelope<T>>(ApiJson.Default, cancellationToken);
            return envelope is null ? default : envelope.Data;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "VocaNova.API GET {RequestUri} failed.", requestUri);
            return default;
        }
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        var data = await GetDataAsync<List<T>>(requestUri, cancellationToken);
        return data ?? new List<T>();
    }

    private async Task<PagedData<T>> GetPagedAsync<T>(
        string requestUri,
        int requestedPage,
        int requestedLimit,
        CancellationToken cancellationToken)
    {
        var empty = new PagedData<T> { Page = requestedPage, Limit = requestedLimit };
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogNonSuccess(requestUri, (int)response.StatusCode);
                return empty;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<PagedEnvelope<T>>(ApiJson.Default, cancellationToken);
            if (envelope is null)
            {
                return empty;
            }

            return new PagedData<T>
            {
                Items = envelope.Data,
                Page = envelope.Pagination?.Page ?? requestedPage,
                Limit = envelope.Pagination?.Limit ?? requestedLimit,
                TotalItems = envelope.Pagination?.TotalItems ?? envelope.Data.Count,
                TotalPages = envelope.Pagination?.TotalPages ?? 1,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "VocaNova.API GET {RequestUri} failed.", requestUri);
            return empty;
        }
    }

    private Task<ApiActionResult> SendActionAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken) =>
        SendActionCoreAsync(() => new HttpRequestMessage(method, requestUri), method, requestUri, cancellationToken);

    private Task<ApiActionResult> SendJsonActionAsync(
        HttpMethod method,
        string requestUri,
        object payload,
        CancellationToken cancellationToken) =>
        SendActionCoreAsync(
            () => new HttpRequestMessage(method, requestUri)
            {
                Content = JsonContent.Create(payload, options: ApiJson.Default),
            },
            method,
            requestUri,
            cancellationToken);

    private Task<ApiActionResult> SendMultipartActionAsync(
        HttpMethod method,
        string requestUri,
        MultipartFormDataContent content,
        CancellationToken cancellationToken) =>
        SendActionCoreAsync(
            () => new HttpRequestMessage(method, requestUri) { Content = content },
            method,
            requestUri,
            cancellationToken);

    private async Task<ApiActionResult> SendActionCoreAsync(
        Func<HttpRequestMessage> requestFactory,
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = requestFactory();
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return ApiActionResult.Ok(statusCode);
            }

            string? message = null;
            try
            {
                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<object>>(ApiJson.Default, cancellationToken);
                message = envelope?.Message ?? envelope?.Errors.FirstOrDefault();
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
            {
                // không parse được body lỗi → dùng message null.
            }

            LogNonSuccess(requestUri, statusCode);
            return ApiActionResult.Fail(statusCode, message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "VocaNova.API {Method} {RequestUri} failed.", method, requestUri);
            return ApiActionResult.Fail(0, null);
        }
    }

    private void LogNonSuccess(string requestUri, int statusCode) =>
        _logger.LogWarning("VocaNova.API {RequestUri} returned {StatusCode}.", requestUri, statusCode);
}
