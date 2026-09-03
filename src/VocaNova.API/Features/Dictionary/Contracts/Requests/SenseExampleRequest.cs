using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed record SenseExampleRequest(
    [property: JsonPropertyName("example_id")] uint? ExampleId,
    [property: JsonPropertyName("example_en")] string? ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi);
