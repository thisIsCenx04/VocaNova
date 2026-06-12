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
}
