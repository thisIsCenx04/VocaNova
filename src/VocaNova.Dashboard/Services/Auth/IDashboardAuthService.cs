namespace VocaNova.Dashboard.Services.Auth;

public interface IDashboardAuthService
{
    /// <summary>Đăng nhập qua VocaNova.API và xác minh tài khoản có quyền admin.</summary>
    Task<DashboardAuthResult> LoginAsync(
        string phone,
        string password,
        CancellationToken cancellationToken = default);

    Task<DashboardAuthActionResult> ForgotPasswordAsync(
        string phone,
        CancellationToken cancellationToken = default);

    Task<DashboardAuthActionResult> ResetPasswordAsync(
        string phone,
        string otpCode,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>Thu hồi refresh token phía backend khi đăng xuất.</summary>
    Task LogoutAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default);
}
