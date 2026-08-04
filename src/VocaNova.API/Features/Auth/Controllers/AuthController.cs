using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Auth.DTOs;
using VocaNova.API.Features.Auth.Services;
using VocaNova.API.Infrastructure.RateLimiting;

namespace VocaNova.API.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthRateLimiter? _authRateLimiter;
    private readonly RateLimitSettings _rateLimitSettings;

    public AuthController(
        IAuthService authService,
        IAuthRateLimiter? authRateLimiter = null,
        IOptions<RateLimitSettings>? rateLimitSettings = null)
    {
        _authService = authService;
        _authRateLimiter = authRateLimiter;
        _rateLimitSettings = rateLimitSettings?.Value ?? new RateLimitSettings();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.CreatedResult(result.Value!, "Registered successfully.");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit(
            "auth:login",
            _rateLimitSettings.LoginPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return this.ErrorResult(Result<TokenResponse>.TooManyRequests("Login rate limit exceeded."));
        }

        var result = await _authService.LoginAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Logged in successfully.");
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit(
            "auth:login",
            _rateLimitSettings.LoginPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return this.ErrorResult(Result<TokenResponse>.TooManyRequests("Login rate limit exceeded."));
        }

        var result = await _authService.GoogleLoginAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Logged in successfully.");
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(
            request,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Token refreshed successfully.");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Logged out successfully.");
    }

    [AllowAnonymous]
    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp(
        [FromBody] OtpSendRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit(
            "auth:otp:send",
            _rateLimitSettings.OtpPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return this.ErrorResult(Result<OtpSendResponse>.TooManyRequests("OTP IP rate limit exceeded."));
        }

        var result = await _authService.SendOtpAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "OTP sent successfully.");
    }

    [AllowAnonymous]
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyOtpAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "OTP verified successfully.");
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Password reset OTP sent successfully.");
    }

    [AllowAnonymous]
    [HttpPost("reset-password/verify-otp")]
    public async Task<IActionResult> VerifyResetOtp(
        [FromBody] OtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyResetOtpAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Password reset OTP verified successfully.");
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Password reset successfully.");
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Password changed successfully.");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserProfileDto>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.GetProfileAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Profile loaded successfully.");
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.DeleteAccountAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value, "Account deleted successfully.");
    }

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserProfileDto>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.UpdateProfileAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Profile updated successfully.");
    }

    [Authorize]
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserProfileDto>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.UploadAvatarAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Avatar uploaded successfully.");
    }

    [Authorize]
    [HttpPut("me/learning-profile")]
    public async Task<IActionResult> UpdateLearningProfile(
        [FromBody] UpdateLearningProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<UserProfileDto>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.UpdateLearningProfileAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Learning profile updated successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }

    private AuthRateLimitResult CheckRateLimit(string policyName, int permitLimit)
    {
        if (_authRateLimiter is null)
        {
            return new AuthRateLimitResult(true, 0);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _authRateLimiter.TryAcquire(
            $"{policyName}:{ipAddress}",
            permitLimit,
            TimeSpan.FromSeconds(_rateLimitSettings.RetryAfterSeconds));
    }

    private void SetRetryAfterHeader(AuthRateLimitResult rateLimitResult)
    {
        Response.Headers["Retry-After"] = rateLimitResult.RetryAfterSeconds.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
