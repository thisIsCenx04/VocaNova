namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Cổng gọi VocaNova.API từ Dashboard. Mọi action/controller gọi qua đây (không dùng HttpClient thô).
/// Token Bearer + auto-refresh được xử lý ở <see cref="BearerTokenHandler"/> trong pipeline.
/// Trả <see cref="ApiResult{T}"/>/<see cref="PagedApiResult{T}"/> — KHÔNG throw cho lỗi nghiệp vụ (4xx).
/// </summary>
public interface IVocaNovaApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default);

    Task<PagedApiResult<T>> GetPagedAsync<T>(string url, CancellationToken cancellationToken = default);

    Task<ApiResult<T>> PostAsync<T>(string url, object? body, CancellationToken cancellationToken = default);

    Task<ApiResult<T>> PutAsync<T>(string url, object? body, CancellationToken cancellationToken = default);

    Task<ApiResult<T>> PatchAsync<T>(string url, object? body, CancellationToken cancellationToken = default);

    Task<ApiResult<T>> DeleteAsync<T>(string url, CancellationToken cancellationToken = default);

    /// <summary>Gửi multipart/form-data (upload ảnh/audio, import CSV). Caller tự dựng <paramref name="content"/>.</summary>
    Task<ApiResult<T>> PostFormAsync<T>(string url, HttpContent content, CancellationToken cancellationToken = default);
}
