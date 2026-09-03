using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.Contracts.Requests;

public sealed record AdminAuditLogQueryRequest(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("user_id")] uint? UserId = null,
    [property: JsonPropertyName("entity")] string? Entity = null);
