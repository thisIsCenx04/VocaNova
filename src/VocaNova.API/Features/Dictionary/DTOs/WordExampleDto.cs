using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordExampleDto(
    [property: JsonPropertyName("example_id")] uint ExampleId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("example_en")] string ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi,
    [property: JsonPropertyName("order")] int Order);
