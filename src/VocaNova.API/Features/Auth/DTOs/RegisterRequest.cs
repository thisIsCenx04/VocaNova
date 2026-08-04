using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Auth.DTOs;

public sealed record RegisterRequest(
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("otp_code")] string? OtpCode,
    [property: JsonPropertyName("date_of_birth")] DateOnly? DateOfBirth = null,
    [property: JsonPropertyName("region_id")] uint? RegionId = null,
    [property: JsonPropertyName("occupation_id")] uint? OccupationId = null,
    [property: JsonPropertyName("education_level_id")] uint? EducationLevelId = null);
