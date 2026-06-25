using System.Text.Json;
using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Cấu hình JSON dùng chung khi giao tiếp với VocaNova.API (API trả/nhận snake_case).
/// </summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
}

/// <summary>Shape response thống nhất của VocaNova.API: { success, data, message, errors }.</summary>
public sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Message { get; set; }

    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
}

/// <summary>Envelope cho endpoint phân trang: data là mảng, kèm khối pagination.</summary>
public sealed class PagedEnvelope<T>
{
    public bool Success { get; set; }

    public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();

    public string? Message { get; set; }

    public PaginationInfo? Pagination { get; set; }
}

// Khối pagination của API serialize bằng policy camelCase mặc định (không phải snake_case như DTO).
public sealed class PaginationInfo
{
    [JsonPropertyName("page")] public int Page { get; set; }

    [JsonPropertyName("limit")] public int Limit { get; set; }

    [JsonPropertyName("totalItems")] public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
}

/// <summary>Kết quả phân trang đã chuẩn hóa cho dashboard dùng (items + metadata trang).</summary>
public sealed class PagedData<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; } = 1;

    public int Limit { get; init; } = 20;

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }
}

/// <summary>Kết quả của một lệnh ghi (delete/restore...) để controller hiển thị thành công/lỗi.</summary>
public sealed record ApiActionResult(bool IsSuccess, int StatusCode, string? Message)
{
    public static ApiActionResult Ok(int statusCode, string? message = null) => new(true, statusCode, message);

    public static ApiActionResult Fail(int statusCode, string? message) => new(false, statusCode, message);
}
