namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record CreateWordCommand(
    string Word,
    string WordKey,
    string? Cefr,
    string? PhoneticUk,
    string? PhoneticUs,
    string? ImageUrl,
    bool IsPhrase,
    IReadOnlyCollection<uint>? TopicIds = null);

public sealed record UpdateWordCommand(
    string Word,
    string WordKey,
    string? Cefr,
    string? PhoneticUk,
    string? PhoneticUs,
    string? ImageUrl,
    bool IsPhrase);

public sealed record SenseExampleInput(
    uint? ExampleId,
    string ExampleEn,
    string? ExampleVi);

public sealed record CreateSenseCommand(
    int SenseOrder,
    string WordClass,
    string EnglishDefinition,
    string? VietnameseMeaning,
    IReadOnlyList<SenseExampleInput>? Examples = null);

public sealed record UpdateSenseCommand(
    int SenseOrder,
    string WordClass,
    string EnglishDefinition,
    string? VietnameseMeaning,
    IReadOnlyList<SenseExampleInput>? Examples = null);

public sealed record CreateTopicCommand(
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    IReadOnlyCollection<uint>? WordIds = null);

public sealed record UpdateTopicCommand(
    string TopicName,
    string? TopicNameVi,
    string? Icon,
    IReadOnlyCollection<uint>? WordIds = null);

public sealed record ImportWordMetadata(
    string? Cefr,
    string? PhoneticUk,
    string? PhoneticUs,
    string? ImageUrl,
    bool? IsPhrase);
