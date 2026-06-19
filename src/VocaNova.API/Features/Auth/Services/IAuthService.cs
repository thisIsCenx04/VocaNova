using VocaNova.API.Common.Results;
using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Features.Auth.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> RegisterAsync(
        RegisterRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result<TokenResponse>> GoogleLoginAsync(
        GoogleLoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result<TokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileDto>> GetProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileDto>> UpdateProfileAsync(
        uint userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileDto>> UploadAvatarAsync(
        uint userId,
        UploadAvatarRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileDto>> UpdateLearningProfileAsync(
        uint userId,
        UpdateLearningProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OtpSendResponse>> SendOtpAsync(
        OtpSendRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OtpVerifyResponse>> VerifyOtpAsync(
        OtpVerifyRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OtpSendResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ChangePasswordAsync(
        uint userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
