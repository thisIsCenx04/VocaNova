
namespace VocaNova.Tests.Auth;

internal static class AuthServiceTestExtensions
{
    public static Task<AuthOperationResult<AuthTokenPair>> RegisterAsync(
        this AuthService service,
        RegisterRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default) =>
        service.RegisterAsync(request.ToCommand(), new SignInContext(deviceInfo, ipAddress), cancellationToken);

    public static Task<AuthOperationResult<AuthTokenPair>> LoginAsync(
        this AuthService service,
        LoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default) =>
        service.LoginAsync(request.ToCommand(), new SignInContext(deviceInfo, ipAddress), cancellationToken);

    public static Task<AuthOperationResult<AuthTokenPair>> GoogleLoginAsync(
        this AuthService service,
        GoogleLoginRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default) =>
        service.GoogleLoginAsync(request.ToCommand(), new SignInContext(deviceInfo, ipAddress), cancellationToken);

    public static Task<AuthOperationResult<AuthTokenPair>> RefreshTokenAsync(
        this AuthService service,
        RefreshTokenRequest request,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default) =>
        service.RefreshTokenAsync(request.ToCommand(), new SignInContext(deviceInfo, ipAddress), cancellationToken);

    public static Task<AuthOperationResult<bool>> LogoutAsync(
        this AuthService service,
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default) =>
        service.LogoutAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<OtpSendResult>> SendOtpAsync(
        this AuthService service,
        OtpSendRequest request,
        CancellationToken cancellationToken = default) =>
        service.SendOtpAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<OtpVerificationResult>> VerifyOtpAsync(
        this AuthService service,
        OtpVerifyRequest request,
        CancellationToken cancellationToken = default) =>
        service.VerifyOtpAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<OtpSendResult>> ForgotPasswordAsync(
        this AuthService service,
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        service.ForgotPasswordAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<OtpVerificationResult>> VerifyResetOtpAsync(
        this AuthService service,
        OtpVerifyRequest request,
        CancellationToken cancellationToken = default) =>
        service.VerifyResetOtpAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<bool>> ResetPasswordAsync(
        this AuthService service,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        service.ResetPasswordAsync(request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<bool>> ChangePasswordAsync(
        this AuthService service,
        uint userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default) =>
        service.ChangePasswordAsync(userId, request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<UserProfile>> UpdateProfileAsync(
        this AuthService service,
        uint userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default) =>
        service.UpdateProfileAsync(userId, request.ToCommand(), cancellationToken);

    public static Task<AuthOperationResult<UserProfile>> UploadAvatarAsync(
        this AuthService service,
        uint userId,
        UploadAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        var content = request.File is null
            ? null
            : request.ToUploadedContent(request.File.OpenReadStream(), userId);
        return service.UploadAvatarAsync(userId, new UploadAvatarCommand(content), cancellationToken);
    }

    public static Task<AuthOperationResult<UserProfile>> UpdateLearningProfileAsync(
        this AuthService service,
        uint userId,
        UpdateLearningProfileRequest request,
        CancellationToken cancellationToken = default) =>
        service.UpdateLearningProfileAsync(userId, request.ToCommand(), cancellationToken);
}
