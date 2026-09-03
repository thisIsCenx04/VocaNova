using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.Contracts.Requests;

public sealed record OtpSendRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("purpose")] string? Purpose);
