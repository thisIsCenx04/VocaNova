using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Models.Api.Stats;

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
        if (filter.TopicId is { } topicId) queryParams["topicId"] = topicId.ToString(CultureInfo.InvariantCulture);

        var uri = QueryHelpers.AddQueryString("api/admin/words", queryParams);
        return GetPagedAsync<WordListItem>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<ApiActionResult> DeleteWordAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/words/{wordId}", cancellationToken);

    public Task<ApiActionResult> RestoreWordAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/words/{wordId}/restore", cancellationToken);

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
                .ReadFromJsonAsync<ApiEnvelope<Models.Api.Dictionary.BulkImportResult>>(ApiJson.Default, cancellationToken);

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

    public Task<PagedData<Models.Api.Users.AdminUserSummary>> GetUsersAsync(UserListFilter filter, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["limit"] = filter.Limit.ToString(CultureInfo.InvariantCulture),
            ["includeDeleted"] = filter.IncludeDeleted ? "true" : "false",
        };
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams["status"] = filter.Status;
        if (!string.IsNullOrWhiteSpace(filter.Search)) queryParams["search"] = filter.Search;

        var uri = QueryHelpers.AddQueryString("api/admin/users", queryParams);
        return GetPagedAsync<Models.Api.Users.AdminUserSummary>(uri, filter.Page, filter.Limit, cancellationToken);
    }

    public Task<Models.Api.Users.AdminUserDetail?> GetUserDetailAsync(uint userId, CancellationToken cancellationToken = default) =>
        GetDataAsync<Models.Api.Users.AdminUserDetail>($"api/admin/users/{userId}", cancellationToken);

    public Task<PagedData<Models.Api.Users.AdminUserTestSession>> GetUserTestHistoryAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default) =>
        GetPagedAsync<Models.Api.Users.AdminUserTestSession>(
            $"api/admin/users/{userId}/test-history?page={page}&limit={limit}", page, limit, cancellationToken);

    public Task<PagedData<Models.Api.Users.AuditLog>> GetUserAuditLogsAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default) =>
        GetPagedAsync<Models.Api.Users.AuditLog>(
            $"api/admin/audit-logs?userId={userId}&page={page}&limit={limit}", page, limit, cancellationToken);

    public Task<ApiActionResult> DeactivateUserAsync(uint userId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/users/{userId}/deactivate", cancellationToken);

    public Task<ApiActionResult> RestoreUserAsync(uint userId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/users/{userId}/restore", cancellationToken);

    public Task<IReadOnlyList<Models.Api.Dictionary.AdminTopic>> GetAdminTopicsAsync(
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
        return GetListAsync<Models.Api.Dictionary.AdminTopic>(uri, cancellationToken);
    }

    public Task<ApiActionResult> CreateTopicAsync(TopicInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Post, "api/admin/topics", TopicPayload(input), cancellationToken);

    public Task<ApiActionResult> UpdateTopicAsync(uint topicId, TopicInput input, CancellationToken cancellationToken = default) =>
        SendJsonActionAsync(HttpMethod.Put, $"api/admin/topics/{topicId}", TopicPayload(input), cancellationToken);

    public Task<ApiActionResult> DeleteTopicAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Delete, $"api/admin/topics/{topicId}", cancellationToken);

    public Task<ApiActionResult> RestoreTopicAsync(uint topicId, CancellationToken cancellationToken = default) =>
        SendActionAsync(HttpMethod.Patch, $"api/admin/topics/{topicId}/restore", cancellationToken);

    public Task<Models.Api.Knn.KnnConfig?> GetKnnConfigAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<Models.Api.Knn.KnnConfig>("api/admin/knn/config", cancellationToken);

    public Task<Models.Api.Knn.KnnRebuildStatus?> GetKnnRebuildStatusAsync(CancellationToken cancellationToken = default) =>
        GetDataAsync<Models.Api.Knn.KnnRebuildStatus>("api/admin/knn/rebuild-status", cancellationToken);

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

    private static object TopicPayload(TopicInput input) => new
    {
        input.TopicName,
        input.TopicNameVi,
        input.Icon,
    };

    private static object SensePayload(SenseInput input) => new
    {
        input.SenseOrder,
        input.WordClass,
        input.EnglishDefinition,
        input.VietnameseMeaning,
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
