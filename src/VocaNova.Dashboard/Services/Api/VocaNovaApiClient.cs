using System.Net.Http.Json;
using System.Text.Json;

namespace VocaNova.Dashboard.Services.Api;

/// <inheritdoc cref="IVocaNovaApiClient"/>
public sealed class VocaNovaApiClient : IVocaNovaApiClient
{
    private const string ConnectionErrorMessage = "Không kết nối được máy chủ. Vui lòng thử lại.";

    private readonly HttpClient _httpClient;
    private readonly ILogger<VocaNovaApiClient> _logger;

    public VocaNovaApiClient(HttpClient httpClient, ILogger<VocaNovaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Get, url, content: null, cancellationToken);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Post, url, JsonBody(body), cancellationToken);

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Put, url, JsonBody(body), cancellationToken);

    public Task<ApiResult<T>> PatchAsync<T>(string url, object? body, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Patch, url, JsonBody(body), cancellationToken);

    public Task<ApiResult<T>> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Delete, url, content: null, cancellationToken);

    public Task<ApiResult<T>> PostFormAsync<T>(string url, HttpContent content, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Post, url, content, cancellationToken);

    public async Task<PagedApiResult<T>> GetPagedAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var envelope = await ReadEnvelopeAsync<IReadOnlyList<T>>(response, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode && envelope is { Success: true })
            {
                return PagedApiResult<T>.Ok(
                    envelope.Data ?? Array.Empty<T>(),
                    envelope.Pagination,
                    envelope.Message,
                    statusCode);
            }

            return PagedApiResult<T>.Fail(
                envelope?.Message ?? response.ReasonPhrase,
                envelope?.Errors,
                statusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "API paged call failed: GET {Url}", url);
            return PagedApiResult<T>.Fail(ConnectionErrorMessage, errors: null, statusCode: 0);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var envelope = await ReadEnvelopeAsync<T>(response, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode && envelope is { Success: true })
            {
                return ApiResult<T>.Ok(envelope.Data, envelope.Message, statusCode);
            }

            return ApiResult<T>.Fail(
                envelope?.Message ?? response.ReasonPhrase,
                envelope?.Errors,
                statusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "API call failed: {Method} {Url}", method, url);
            return ApiResult<T>.Fail(ConnectionErrorMessage, errors: null, statusCode: 0);
        }
    }

    private static HttpContent? JsonBody(object? body)
        => body is null ? null : JsonContent.Create(body, body.GetType(), options: ApiJson.Default);

    private static async Task<ApiEnvelope<T>?> ReadEnvelopeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(ApiJson.Default, cancellationToken);
        }
        catch (JsonException)
        {
            return null; // response không phải envelope JSON (vd 500 HTML) → coi như fail
        }
        catch (NotSupportedException)
        {
            return null; // content-type không đọc được dạng JSON
        }
    }
}
