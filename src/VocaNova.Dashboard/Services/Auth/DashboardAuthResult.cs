using VocaNova.Dashboard.Models.Auth;

namespace VocaNova.Dashboard.Services.Auth;

/// <summary>Kết quả đăng nhập dashboard: thành công kèm token + user, hoặc thất bại kèm lý do.</summary>
public sealed class DashboardAuthResult
{
    public bool IsSuccess { get; private init; }

    public DashboardUser? User { get; private init; }

    public string? AccessToken { get; private init; }

    public string? RefreshToken { get; private init; }

    public int ExpiresIn { get; private init; }

    public string? Error { get; private init; }

    public static DashboardAuthResult Success(
        DashboardUser user,
        string accessToken,
        string refreshToken,
        int expiresIn) => new()
        {
            IsSuccess = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
        };

    public static DashboardAuthResult Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error,
    };
}
