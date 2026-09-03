using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Services.IServices;

public interface IAuthService
{
    Task<AuthOperationResult<AuthTokenPair>> RegisterAsync(
        RegisterCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<AuthTokenPair>> LoginAsync(
        LoginCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<AuthTokenPair>> GoogleLoginAsync(
        GoogleLoginCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<AuthTokenPair>> RefreshTokenAsync(
        RefreshTokenCommand command,
        SignInContext signInContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<bool>> LogoutAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<UserProfile>> GetProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<UserProfile>> UpdateProfileAsync(
        uint userId,
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<UserProfile>> UploadAvatarAsync(
        uint userId,
        UploadAvatarCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<UserProfile>> UpdateLearningProfileAsync(
        uint userId,
        UpdateLearningProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<OtpSendResult>> SendOtpAsync(
        OtpSendCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<OtpVerificationResult>> VerifyOtpAsync(
        OtpVerifyCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<OtpSendResult>> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<OtpVerificationResult>> VerifyResetOtpAsync(
        OtpVerifyCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<bool>> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<bool>> ChangePasswordAsync(
        uint userId,
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult<bool>> DeleteAccountAsync(
        uint userId,
        CancellationToken cancellationToken = default);
}
