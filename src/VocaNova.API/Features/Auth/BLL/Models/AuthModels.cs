using System.Security.Claims;

namespace VocaNova.API.Features.Auth.BLL.Models;

public sealed record AuthTokenPair(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer");

public sealed record OtpSendResult(int ExpiresIn);

public sealed record OtpVerificationResult(bool Verified);

public sealed record UserProfile(
    uint UserId,
    string? Phone,
    string FullName,
    string? AvatarUrl,
    string Role,
    string Status,
    LearningProfile? LearningProfile);

public sealed record LearningProfile(
    uint? AgeRangeId,
    uint? RegionId,
    uint? OccupationId,
    uint? EducationLevelId,
    uint? LearningPurposeId);

public sealed record UploadedContent(
    string FileName,
    string ContentType,
    long Length,
    Stream Content,
    uint OwnerId = 0);

public sealed record StoredMedia(string ObjectKey, string Url);

public sealed record GoogleIdentity(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? Picture);

public sealed record AuthPrincipal(uint UserId, string? Role, ClaimsPrincipal Principal)
{
    public Claim? FindFirst(string type) => Principal.FindFirst(type);

    public string? FindFirstValue(string type) => Principal.FindFirst(type)?.Value;

    public bool IsInRole(string role) => Principal.IsInRole(role);
}

public sealed record AuthRateLimitDecision(bool IsAllowed, int RetryAfterSeconds);

public sealed class AuthTokenOptions
{
    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }
}

public sealed class AuthRateLimitOptions
{
    private int _otpPerMinutePerPhone = 1;
    private int _otpPerMinutePerIp = 1;
    private int _loginPerMinutePerIp = 10;
    private int _retryAfterSeconds = 60;

    public int OtpPerMinutePerPhone
    {
        get => _otpPerMinutePerPhone;
        set => _otpPerMinutePerPhone = UseConfiguredOrDefault(value, _otpPerMinutePerPhone);
    }

    public int OtpPerMinutePerIp
    {
        get => _otpPerMinutePerIp;
        set => _otpPerMinutePerIp = UseConfiguredOrDefault(value, _otpPerMinutePerIp);
    }

    public int LoginPerMinutePerIp
    {
        get => _loginPerMinutePerIp;
        set => _loginPerMinutePerIp = UseConfiguredOrDefault(value, _loginPerMinutePerIp);
    }

    public int RetryAfterSeconds
    {
        get => _retryAfterSeconds;
        set => _retryAfterSeconds = UseConfiguredOrDefault(value, _retryAfterSeconds);
    }

    private static int UseConfiguredOrDefault(int configuredValue, int defaultValue) =>
        configuredValue > 0 ? configuredValue : defaultValue;
}
