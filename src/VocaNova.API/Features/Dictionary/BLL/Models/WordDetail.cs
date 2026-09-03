namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record WordDetail(
    uint WordId,
    string Word,
    string WordKey,
    string? Cefr,
    string? PhoneticUk,
    string? PhoneticUs,
    string? ImageUrl,
    bool IsPhrase,
    IReadOnlyCollection<WordSense> Senses,
    IReadOnlyCollection<WordExample> Examples,
    IReadOnlyCollection<WordRelation> Relations,
    IReadOnlyCollection<WordAudio> Audio,
    IReadOnlyCollection<WordDerivedForm> DerivedForms,
    IReadOnlyCollection<WordIdiom> Idioms,
    IReadOnlyCollection<WordTopic> Topics,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record WordSense(
    uint SenseId,
    int Order,
    string WordClass,
    string EnglishDefinition,
    string? VietnameseMeaning,
    IReadOnlyCollection<WordExample> Examples,
    IReadOnlyCollection<WordRelation> Relations);

public sealed record WordExample(
    uint ExampleId,
    uint? SenseId,
    string ExampleEn,
    string? ExampleVi,
    int Order);

public sealed record WordRelation(
    uint RelationId,
    uint? SenseId,
    string RelationType,
    string RelatedWord,
    uint? LinkedWordId,
    bool IsQuizEligible);

public sealed record WordAudio(
    uint AudioId,
    string Accent,
    string Source,
    string Url,
    string Status);

public sealed record WordDerivedForm(
    uint DerivedId,
    string DerivedWord,
    uint? LinkedWordId,
    string? WordClass);

public sealed record WordIdiom(
    uint IdiomId,
    string IdiomText,
    string? MeaningEn,
    string? MeaningVi);

public sealed record WordTopic(
    uint TopicId,
    string Name,
    string? NameVi,
    string? Icon);
