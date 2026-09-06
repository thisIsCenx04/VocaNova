namespace VocaNova.Dashboard.Services.Auth;

public sealed record DashboardAuthActionResult(bool IsSuccess, string? Message)
{
    public static DashboardAuthActionResult Ok(string? message = null) => new(true, message);

    public static DashboardAuthActionResult Fail(string? message) => new(false, message);
}
