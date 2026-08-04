using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record OtpSendRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("purpose")] string? Purpose);
