using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Gắn access token (lấy từ auth cookie) vào mọi request tới VocaNova.API.
/// Access token sống 15 phút trong khi cookie sống 30 phút → khi gặp 401:
/// gọi <c>POST /api/auth/refresh</c>, cập nhật token mới vào cookie, rồi retry request đúng 1 lần.
/// Refresh thất bại → giữ nguyên 401 để controller redirect về /login.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";

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
        if (httpContext is null)
        {
            // Ngoài request scope (vd background) → không có cookie để đính token.
            return await base.SendAsync(request, cancellationToken);
        }

        var accessToken = await httpContext.GetTokenAsync(AccessTokenName);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Buffer body trước khi gửi để có thể tái tạo request khi retry.
        byte[]? bodyBytes = null;
        if (request.Content is not null)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var refreshToken = await httpContext.GetTokenAsync(RefreshTokenName);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshed = await TryRefreshAsync(refreshToken, cancellationToken);
        if (refreshed is null)
        {
            return response; // refresh fail → giữ 401
        }

        await UpdateCookieTokensAsync(httpContext, refreshed);

        var retry = CloneForRetry(request, bodyBytes, refreshed.AccessToken);
        response.Dispose();
        return await base.SendAsync(retry, cancellationToken);
    }

    private async Task<RefreshedTokens?> TryRefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = _options.BaseUrl.TrimEnd('/') + "/api/auth/refresh";

            using var refreshResponse = await client.PostAsJsonAsync(
                url,
                new RefreshTokenRequest(refreshToken),
                ApiJson.Default,
                cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var envelope = await refreshResponse.Content
                .ReadFromJsonAsync<ApiEnvelope<TokenPayload>>(ApiJson.Default, cancellationToken);

            var data = envelope?.Data;
            if (data is null
                || string.IsNullOrWhiteSpace(data.AccessToken)
                || string.IsNullOrWhiteSpace(data.RefreshToken))
            {
                return null;
            }

            return new RefreshedTokens(data.AccessToken, data.RefreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh access token failed.");
            return null;
        }
    }

    private static async Task UpdateCookieTokensAsync(HttpContext httpContext, RefreshedTokens tokens)
    {
        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return;
        }

        var properties = authResult.Properties ?? new AuthenticationProperties();
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = AccessTokenName, Value = tokens.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName, Value = tokens.RefreshToken },
        });

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authResult.Principal,
            properties);
    }

    private static HttpRequestMessage CloneForRetry(HttpRequestMessage original, byte[]? bodyBytes, string accessToken)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
        };

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (bodyBytes is not null)
        {
            clone.Content = new ByteArrayContent(bodyBytes);
            if (original.Content is not null)
            {
                foreach (var contentHeader in original.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(contentHeader.Key, contentHeader.Value);
                }
            }
        }

        return clone;
    }

    private sealed record RefreshTokenRequest(string RefreshToken);

    private sealed record TokenPayload(string? AccessToken, string? RefreshToken, int ExpiresIn, string? TokenType);

    private sealed record RefreshedTokens(string AccessToken, string RefreshToken);
}
