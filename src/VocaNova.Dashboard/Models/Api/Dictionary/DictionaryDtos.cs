namespace VocaNova.Dashboard.Models.Api.Dictionary;

// Map từ envelope API qua ApiJson.Default (SnakeCaseLower) — không cần [JsonPropertyName] vì không có field chứa số.

public sealed class WordSummaryDto
{
    public uint WordId { get; set; }

    public string Word { get; set; } = string.Empty;

    public string? Phonetic { get; set; }

    public string? Cefr { get; set; }

    public string? PrimaryMeaning { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Chỉ có khi dùng admin word list (G1); `GET /api/words` công khai không trả → null.</summary>
    public string? Status { get; set; }
}

public sealed class TopicSummaryDto
{
    public uint TopicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameVi { get; set; }

    public string? Icon { get; set; }

    public int WordCount { get; set; }
}

public sealed class WordDetailDto
{
    public uint WordId { get; set; }
    public string Word { get; set; } = string.Empty;
    public string WordKey { get; set; } = string.Empty;
    public string? Cefr { get; set; }
    public string? PhoneticUk { get; set; }
    public string? PhoneticUs { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPhrase { get; set; }
    public IReadOnlyList<WordSenseDto> Senses { get; set; } = Array.Empty<WordSenseDto>();
    public IReadOnlyList<WordExampleDto> Examples { get; set; } = Array.Empty<WordExampleDto>();
    public IReadOnlyList<WordRelationDto> Relations { get; set; } = Array.Empty<WordRelationDto>();
    public IReadOnlyList<WordAudioDto> Audio { get; set; } = Array.Empty<WordAudioDto>();
    public IReadOnlyList<WordTopicDto> Topics { get; set; } = Array.Empty<WordTopicDto>();
}

public sealed class WordSenseDto
{
    public uint SenseId { get; set; }
    public int Order { get; set; }
    public string WordClass { get; set; } = string.Empty;
    public string EnglishDefinition { get; set; } = string.Empty;
    public string? VietnameseMeaning { get; set; }
    public IReadOnlyList<WordExampleDto> Examples { get; set; } = Array.Empty<WordExampleDto>();
    public IReadOnlyList<WordRelationDto> Relations { get; set; } = Array.Empty<WordRelationDto>();
}

public sealed class WordExampleDto
{
    public uint ExampleId { get; set; }
    public uint? SenseId { get; set; }
    public string ExampleEn { get; set; } = string.Empty;
    public string? ExampleVi { get; set; }
    public int Order { get; set; }
}

public sealed class WordRelationDto
{
    public uint RelationId { get; set; }
    public uint? SenseId { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public string RelatedWord { get; set; } = string.Empty;
    public uint? LinkedWordId { get; set; }
    public bool IsQuizEligible { get; set; }
}

public sealed class WordAudioDto
{
    public uint AudioId { get; set; }
    public string Accent { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class WordTopicDto
{
    public uint TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameVi { get; set; }
    public string? Icon { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class BulkImportResultDto
{
    public int ImportedWords { get; set; }
    public int ImportedSenses { get; set; }
    public int Skipped { get; set; }
    public IReadOnlyList<BulkImportErrorDto> Errors { get; set; } = Array.Empty<BulkImportErrorDto>();
}

public sealed class BulkImportErrorDto
{
    public int Row { get; set; }
    public string Column { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
