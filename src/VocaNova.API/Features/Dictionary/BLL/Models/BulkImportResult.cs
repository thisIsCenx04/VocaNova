namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record BulkImportResult(
    int ImportedWords,
    int ImportedSenses,
    int Skipped,
    IReadOnlyCollection<BulkImportError> Errors,
    int UpdatedWords = 0,
    int ImportedTopics = 0,
    int ImportedExamples = 0);

public sealed record BulkImportError(int Row, string Column, string Message);
