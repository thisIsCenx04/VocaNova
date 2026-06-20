using System.Text.Json;

namespace VocaNova.Dashboard.Services.Api;

/// <summary>
/// Cấu hình JSON dùng chung khi giao tiếp với VocaNova.API.
/// API trả/nhận snake_case → dùng SnakeCaseLower để khỏi rải [JsonPropertyName] khắp nơi.
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
