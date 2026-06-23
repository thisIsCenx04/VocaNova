namespace VocaNova.Dashboard.Services.Auth;

public interface IDashboardAuthService
{
    Task<DashboardAuthResult> LoginAsync(
        string phone,
        string password,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>Gửi OTP đặt lại mật khẩu (`POST /api/auth/forgot-password`).</summary>
    Task<DashboardActionResult> ForgotPasswordAsync(
        string phone,
        CancellationToken cancellationToken = default);

    /// <summary>Đặt lại mật khẩu bằng OTP (`POST /api/auth/reset-password`).</summary>
    Task<DashboardActionResult> ResetPasswordAsync(
        string phone,
        string otpCode,
        string newPassword,
        CancellationToken cancellationToken = default);
}
