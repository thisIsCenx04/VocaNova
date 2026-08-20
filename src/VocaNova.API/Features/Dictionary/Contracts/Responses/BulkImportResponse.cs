using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record BulkImportResponse(
    [property: JsonPropertyName("imported_words")] int ImportedWords,
    [property: JsonPropertyName("imported_senses")] int ImportedSenses,
    [property: JsonPropertyName("skipped")] int Skipped,
    [property: JsonPropertyName("errors")] IReadOnlyCollection<BulkImportErrorResponse> Errors,
    [property: JsonPropertyName("updated_words")] int UpdatedWords = 0,
    [property: JsonPropertyName("imported_topics")] int ImportedTopics = 0,
    [property: JsonPropertyName("imported_examples")] int ImportedExamples = 0);
