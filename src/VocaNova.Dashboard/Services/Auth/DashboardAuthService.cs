using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VocaNova.Dashboard.Models.Auth;

namespace VocaNova.Dashboard.Services.Auth;

public sealed class DashboardAuthService : IDashboardAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly HashSet<string> DashboardRoles = new(StringComparer.Ordinal)
    {
        "admin",
        "super_admin",
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardAuthService> _logger;

    public DashboardAuthService(HttpClient httpClient, ILogger<DashboardAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DashboardAuthResult> LoginAsync(
        string phone,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var loginResponse = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginApiRequest(phone, password),
            JsonOptions,
            cancellationToken);

        var tokenEnvelope = await ReadEnvelopeAsync<TokenApiResponse>(loginResponse, cancellationToken);
        if (!loginResponse.IsSuccessStatusCode || tokenEnvelope?.Data is null)
        {
            return DashboardAuthResult.Failure(GetErrorMessage(tokenEnvelope, "Invalid phone or password."));
        }

        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue(
            tokenEnvelope.Data.TokenType,
            tokenEnvelope.Data.AccessToken);

        using var profileResponse = await _httpClient.SendAsync(profileRequest, cancellationToken);
        var profileEnvelope = await ReadEnvelopeAsync<UserProfileApiResponse>(profileResponse, cancellationToken);
        if (!profileResponse.IsSuccessStatusCode || profileEnvelope?.Data is null)
        {
            return DashboardAuthResult.Failure(GetErrorMessage(profileEnvelope, "Unable to load account profile."));
        }

        if (!DashboardRoles.Contains(profileEnvelope.Data.Role))
        {
            await LogoutAsync(
                tokenEnvelope.Data.AccessToken,
                tokenEnvelope.Data.RefreshToken,
                cancellationToken);

            return DashboardAuthResult.Failure("Dashboard access requires an admin account.");
        }

        var user = new DashboardUser(
            profileEnvelope.Data.UserId,
            profileEnvelope.Data.Phone,
            profileEnvelope.Data.DisplayName,
            profileEnvelope.Data.AvatarUrl,
            profileEnvelope.Data.Role,
            profileEnvelope.Data.Status);

        return DashboardAuthResult.Success(
            user,
            tokenEnvelope.Data.AccessToken,
            tokenEnvelope.Data.RefreshToken,
            tokenEnvelope.Data.ExpiresIn);
    }

    public async Task LogoutAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(
                    new RefreshTokenApiRequest(refreshToken),
                    options: JsonOptions),
            };

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var _ = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Unable to revoke refresh token during dashboard logout.");
        }
    }

    private static async Task<ApiEnvelope<T>?> ReadEnvelopeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string GetErrorMessage<T>(ApiEnvelope<T>? envelope, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(envelope?.Message))
        {
            return envelope.Message;
        }

        var firstError = envelope?.Errors?.FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstError) ? fallback : firstError;
    }

    private sealed record LoginApiRequest(
        [property: JsonPropertyName("phone")] string Phone,
        [property: JsonPropertyName("password")] string Password);

    private sealed record RefreshTokenApiRequest(
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record TokenApiResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);

    private sealed record UserProfileApiResponse(
        [property: JsonPropertyName("user_id")] uint UserId,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("status")] string Status);

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Message { get; set; }

        public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
    }
}
