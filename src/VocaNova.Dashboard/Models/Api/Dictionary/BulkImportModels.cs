using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Dictionary;

// Mirror BulkImportResultDto/BulkImportErrorDto của VocaNova.API (POST /api/admin/words/import).

public sealed record BulkImportResult(
    [property: JsonPropertyName("imported_words")] int ImportedWords,
    [property: JsonPropertyName("imported_senses")] int ImportedSenses,
    [property: JsonPropertyName("skipped")] int Skipped,
    [property: JsonPropertyName("errors")] IReadOnlyList<BulkImportError> Errors);

public sealed record BulkImportError(
    [property: JsonPropertyName("row")] int Row,
    [property: JsonPropertyName("column")] string Column,
    [property: JsonPropertyName("message")] string Message);
