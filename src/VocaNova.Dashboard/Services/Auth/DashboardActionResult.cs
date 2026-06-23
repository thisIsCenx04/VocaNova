namespace VocaNova.Dashboard.Services.Auth;

/// <summary>Kết quả 1 call auth ẩn danh (forgot/reset password) — phẳng hóa cho controller.</summary>
public sealed class DashboardActionResult
{
    private DashboardActionResult(bool isSuccess, string? message, int statusCode, int expiresIn)
    {
        IsSuccess = isSuccess;
        Message = message;
        StatusCode = statusCode;
        ExpiresIn = expiresIn;
    }

    public bool IsSuccess { get; }

    public string? Message { get; }

    public int StatusCode { get; }

    /// <summary>Số giây OTP còn hiệu lực (chỉ có ở forgot-password).</summary>
    public int ExpiresIn { get; }

    public static DashboardActionResult Ok(int statusCode, int expiresIn = 0)
        => new(true, null, statusCode, expiresIn);

    public static DashboardActionResult Fail(string? message, int statusCode)
        => new(false, message, statusCode, 0);
}
