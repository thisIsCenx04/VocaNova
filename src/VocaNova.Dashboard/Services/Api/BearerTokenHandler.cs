using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// F055.1 — gắn Bearer access token (lấy từ cookie auth) vào mọi request tới VocaNova.API.
/// Khi gặp 401: gọi POST /api/auth/refresh, cập nhật token trong cookie rồi thử lại 1 lần.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private const string AccessTokenProperty = "access_token";
    private const string RefreshTokenProperty = "refresh_token";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DashboardApiOptions _options;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<DashboardApiOptions> options,
        ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var accessToken = httpContext is null
            ? null
            : await httpContext.GetTokenAsync(AccessTokenProperty);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || httpContext is null)
        {
            return response;
        }

        var refreshToken = await httpContext.GetTokenAsync(RefreshTokenProperty);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshed = await TryRefreshAsync(refreshToken, cancellationToken);
        if (refreshed is null)
        {
            return response;
        }

        await UpdateCookieTokensAsync(httpContext, refreshed);

        // Thử lại request gốc với access token mới.
        response.Dispose();
        using var retryRequest = await CloneRequestAsync(request);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<RefreshedTokens?> TryRefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            // Client riêng (không qua handler này) để tránh đệ quy refresh.
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            using var response = await client.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshRequest(refreshToken),
                ApiJson.Default,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var envelope = await response.Content
                .ReadFromJsonAsync<ApiEnvelope<TokenResponse>>(ApiJson.Default, cancellationToken);
            if (envelope?.Data is null
                || string.IsNullOrWhiteSpace(envelope.Data.AccessToken)
                || string.IsNullOrWhiteSpace(envelope.Data.RefreshToken))
            {
                return null;
            }

            return new RefreshedTokens(envelope.Data.AccessToken, envelope.Data.RefreshToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Token refresh failed during dashboard API call.");
            return null;
        }
    }

    private static async Task UpdateCookieTokensAsync(HttpContext httpContext, RefreshedTokens tokens)
    {
        // Cookie chưa được gửi đi (action chưa render response) → re-issue được token mới.
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null || authResult.Properties is null)
        {
            return;
        }

        authResult.Properties.UpdateTokenValue(AccessTokenProperty, tokens.AccessToken);
        authResult.Properties.UpdateTokenValue(RefreshTokenProperty, tokens.RefreshToken);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authResult.Principal,
            authResult.Properties);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
        };

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private sealed record RefreshRequest(
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record RefreshedTokens(string AccessToken, string RefreshToken);
}
