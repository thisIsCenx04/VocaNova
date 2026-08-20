using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Services;
using VocaNova.API.Features.Auth.Contracts.Requests;
using VocaNova.API.Features.Auth.Mappings;

namespace VocaNova.API.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthRateLimiter? _authRateLimiter;
    private readonly AuthRateLimitOptions _rateLimitOptions;

    public AuthController(
        IAuthService authService,
        IAuthRateLimiter? authRateLimiter = null,
        IOptions<AuthRateLimitOptions>? rateLimitOptions = null)
    {
        _authService = authService;
        _authRateLimiter = authRateLimiter;
        _rateLimitOptions = rateLimitOptions?.Value ?? new AuthRateLimitOptions();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            request.ToCommand(),
            GetSignInContext(),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponseFormatter.Created(result.Value!.ToResponse(), "Registered successfully."))
            : ErrorResponse(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit("auth:login", _rateLimitOptions.LoginPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return ErrorResponse(AuthOperationResult<AuthTokenPair>.TooManyRequests("Login rate limit exceeded."));
        }

        var result = await _authService.LoginAsync(
            request.ToCommand(),
            GetSignInContext(),
            cancellationToken);

        return TokenResponse(result, "Logged in successfully.");
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit("auth:login", _rateLimitOptions.LoginPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return ErrorResponse(AuthOperationResult<AuthTokenPair>.TooManyRequests("Login rate limit exceeded."));
        }

        var result = await _authService.GoogleLoginAsync(
            request.ToCommand(),
            GetSignInContext(),
            cancellationToken);

        return TokenResponse(result, "Logged in successfully.");
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(
            request.ToCommand(),
            GetSignInContext(),
            cancellationToken);

        return TokenResponse(result, "Token refreshed successfully.");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Logged out successfully."))
            : ErrorResponse(result);
    }

    [AllowAnonymous]
    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp(
        [FromBody] OtpSendRequest request,
        CancellationToken cancellationToken)
    {
        var rateLimitResult = CheckRateLimit("auth:otp:send", _rateLimitOptions.OtpPerMinutePerIp);
        if (!rateLimitResult.IsAllowed)
        {
            SetRetryAfterHeader(rateLimitResult);
            return ErrorResponse(AuthOperationResult<OtpSendResult>.TooManyRequests("OTP IP rate limit exceeded."));
        }

        var result = await _authService.SendOtpAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "OTP sent successfully."))
            : ErrorResponse(result);
    }

    [AllowAnonymous]
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] OtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyOtpAsync(request.ToCommand(), cancellationToken);
        return OtpVerificationResponse(result, "OTP verified successfully.");
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), "Password reset OTP sent successfully."))
            : ErrorResponse(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password/verify-otp")]
    public async Task<IActionResult> VerifyResetOtp(
        [FromBody] OtpVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyResetOtpAsync(request.ToCommand(), cancellationToken);
        return OtpVerificationResponse(result, "Password reset OTP verified successfully.");
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Password reset successfully."))
            : ErrorResponse(result);
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ErrorResponse(AuthOperationResult<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.ChangePasswordAsync(userId, request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Password changed successfully."))
            : ErrorResponse(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ErrorResponse(AuthOperationResult<UserProfile>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.GetProfileAsync(userId, cancellationToken);
        return ProfileResponse(result, "Profile loaded successfully.");
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ErrorResponse(AuthOperationResult<bool>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.DeleteAccountAsync(userId, cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value, "Account deleted successfully."))
            : ErrorResponse(result);
    }

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ErrorResponse(AuthOperationResult<UserProfile>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.UpdateProfileAsync(userId, request.ToCommand(), cancellationToken);
        return ProfileResponse(result, "Profile updated successfully.");
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
            return ErrorResponse(AuthOperationResult<UserProfile>.Unauthorized("Unauthorized."));
        }

        await using var stream = request.File?.OpenReadStream();
        var result = await _authService.UploadAvatarAsync(
            userId,
            new UploadAvatarCommand(request.ToUploadedContent(stream, userId)),
            cancellationToken);

        return ProfileResponse(result, "Avatar uploaded successfully.");
    }

    [Authorize]
    [HttpPut("me/learning-profile")]
    public async Task<IActionResult> UpdateLearningProfile(
        [FromBody] UpdateLearningProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ErrorResponse(AuthOperationResult<UserProfile>.Unauthorized("Unauthorized."));
        }

        var result = await _authService.UpdateLearningProfileAsync(userId, request.ToCommand(), cancellationToken);
        return ProfileResponse(result, "Learning profile updated successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }

    private SignInContext GetSignInContext() =>
        new(Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());

    private AuthRateLimitDecision CheckRateLimit(string policyName, int permitLimit)
    {
        if (_authRateLimiter is null)
        {
            return new AuthRateLimitDecision(true, 0);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _authRateLimiter.TryAcquire(
            $"{policyName}:{ipAddress}",
            permitLimit,
            TimeSpan.FromSeconds(_rateLimitOptions.RetryAfterSeconds));
    }

    private void SetRetryAfterHeader(AuthRateLimitDecision rateLimitResult)
    {
        Response.Headers["Retry-After"] = rateLimitResult.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private IActionResult TokenResponse(AuthOperationResult<AuthTokenPair> result, string message) =>
        result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), message))
            : ErrorResponse(result);

    private IActionResult OtpVerificationResponse(
        AuthOperationResult<OtpVerificationResult> result,
        string message) =>
        result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), message))
            : ErrorResponse(result);

    private IActionResult ProfileResponse(AuthOperationResult<UserProfile> result, string message) =>
        result.IsSuccess
            ? Ok(ApiResponseFormatter.Success(result.Value!.ToResponse(), message))
            : ErrorResponse(result);

    private IActionResult ErrorResponse<T>(AuthOperationResult<T> result)
    {
        var message = result.Error ?? "Request failed.";
        return StatusCode(
            GetStatusCode(result.ErrorKind),
            ApiResponseFormatter.Error(message, [message]));
    }

    private static int GetStatusCode(AuthErrorKind? errorKind) =>
        errorKind switch
        {
            AuthErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            AuthErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            AuthErrorKind.NotFound => StatusCodes.Status404NotFound,
            AuthErrorKind.Conflict => StatusCodes.Status409Conflict,
            AuthErrorKind.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest,
        };
}
