using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record BulkImportErrorDto(
    [property: JsonPropertyName("row")] int Row,
    [property: JsonPropertyName("column")] string Column,
    [property: JsonPropertyName("message")] string Message);
