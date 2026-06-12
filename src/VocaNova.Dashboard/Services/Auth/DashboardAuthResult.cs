using VocaNova.Dashboard.Models.Auth;

namespace VocaNova.Dashboard.Services.Auth;

public sealed class DashboardAuthResult
{
    private DashboardAuthResult(
        bool isSuccess,
        DashboardUser? user,
        string? accessToken,
        string? refreshToken,
        int expiresIn,
        string? error)
    {
        IsSuccess = isSuccess;
        User = user;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresIn = expiresIn;
        Error = error;
    }

    public bool IsSuccess { get; }

    public DashboardUser? User { get; }

    public string? AccessToken { get; }

    public string? RefreshToken { get; }

    public int ExpiresIn { get; }

    public string? Error { get; }

    public static DashboardAuthResult Success(
        DashboardUser user,
        string accessToken,
        string refreshToken,
        int expiresIn)
    {
        return new DashboardAuthResult(true, user, accessToken, refreshToken, expiresIn, null);
    }

    public static DashboardAuthResult Failure(string error)
    {
        return new DashboardAuthResult(false, null, null, null, 0, error);
    }
}
