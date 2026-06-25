using System.Text.Json;

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
