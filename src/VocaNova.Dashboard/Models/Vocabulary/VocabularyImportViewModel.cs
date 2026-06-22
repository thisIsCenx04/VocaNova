namespace VocaNova.Dashboard.Models.Vocabulary;

public sealed class VocabularyImportViewModel
{
    public const long MaxFileBytes = 5 * 1024 * 1024;

    public string TemplateUrl { get; init; } = "/templates/words-import-template.csv";
}
