using VocaNova.Dashboard.Models.Api.Dictionary;

namespace VocaNova.Dashboard.Models.Vocabulary;

public sealed class VocabularyDetailViewModel
{
    public WordDetailDto? Word { get; init; }
    public bool Loaded { get; init; }
    public string? ErrorMessage { get; init; }

    // Current API routes exist, but the service intentionally returns "not supported"
    // until the database schema gains soft-delete support for senses.
    public bool SenseDeleteAvailable { get; init; }

    // Examples are readable in WordDetailDto; mutation endpoints are not implemented yet.
    public bool ExampleMutationAvailable { get; init; }
}

public sealed class SenseInputModel
{
    public int SenseOrder { get; set; }
    public string? WordClass { get; set; }
    public string? EnglishDefinition { get; set; }
    public string? VietnameseMeaning { get; set; }
}
