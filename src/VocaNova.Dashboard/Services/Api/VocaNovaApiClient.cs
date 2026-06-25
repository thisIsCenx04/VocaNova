using System.Net.Http.Json;
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

    private async Task<T?> GetDataAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "VocaNova.API GET {RequestUri} returned {StatusCode}.",
                    requestUri,
                    (int)response.StatusCode);
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
}
