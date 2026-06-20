namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Envelope chuẩn của VocaNova.API: { success, data, message, errors, pagination }.
/// Dùng để deserialize response thô; controller/service nên dùng <see cref="ApiResult{T}"/>.
/// </summary>
internal sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Message { get; set; }

    public IReadOnlyList<string>? Errors { get; set; }

    public PaginationInfo? Pagination { get; set; }
}

/// <summary>Thông tin phân trang (khớp envelope API).</summary>
public sealed class PaginationInfo
{
    public int Page { get; set; }

    public int Limit { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}

/// <summary>
/// Kết quả 1 call API đã được "phẳng hóa" cho tầng controller.
/// KHÔNG throw cho lỗi nghiệp vụ (4xx) — đọc <see cref="IsSuccess"/> + <see cref="StatusCode"/>.
/// </summary>
public sealed class ApiResult<T>
{
    public bool IsSuccess { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public int StatusCode { get; init; }

    public static ApiResult<T> Ok(T? data, string? message, int statusCode) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message,
        StatusCode = statusCode,
    };

    public static ApiResult<T> Fail(string? message, IReadOnlyList<string>? errors, int statusCode) => new()
    {
        IsSuccess = false,
        Message = message,
        Errors = errors ?? Array.Empty<string>(),
        StatusCode = statusCode,
    };
}

/// <summary>Kết quả 1 call API trả danh sách có phân trang.</summary>
public sealed class PagedApiResult<T>
{
    public bool IsSuccess { get; init; }

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public PaginationInfo? Pagination { get; init; }

    public string? Message { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public int StatusCode { get; init; }

    public static PagedApiResult<T> Ok(IReadOnlyList<T> items, PaginationInfo? pagination, string? message, int statusCode) => new()
    {
        IsSuccess = true,
        Items = items,
        Pagination = pagination,
        Message = message,
        StatusCode = statusCode,
    };

    public static PagedApiResult<T> Fail(string? message, IReadOnlyList<string>? errors, int statusCode) => new()
    {
        IsSuccess = false,
        Message = message,
        Errors = errors ?? Array.Empty<string>(),
        StatusCode = statusCode,
    };
}
