using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record BulkImportErrorResponse(
    [property: JsonPropertyName("row")] int Row,
    [property: JsonPropertyName("column")] string Column,
    [property: JsonPropertyName("message")] string Message);
