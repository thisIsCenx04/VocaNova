using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.Dictionary;

// Mirror WordDetailDto của VocaNova.API (GET /api/words/{id}).

public sealed record WordDetail(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("word_key")] string WordKey,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("phonetic_us")] string? PhoneticUs,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("is_phrase")] bool IsPhrase,
    [property: JsonPropertyName("senses")] IReadOnlyList<WordSenseDetail> Senses,
    [property: JsonPropertyName("relations")] IReadOnlyList<WordRelation> Relations,
    [property: JsonPropertyName("audio")] IReadOnlyList<WordAudio> Audio,
    [property: JsonPropertyName("topics")] IReadOnlyList<WordTopic> Topics,
    [property: JsonPropertyName("status")] string Status = "active",
    [property: JsonPropertyName("created_at")] DateTime CreatedAt = default,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt = default);

public sealed record WordSenseDetail(
    [property: JsonPropertyName("sense_id")] uint SenseId,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("word_class")] string WordClass,
    [property: JsonPropertyName("english_definition")] string EnglishDefinition,
    [property: JsonPropertyName("vietnamese_meaning")] string? VietnameseMeaning,
    [property: JsonPropertyName("examples")] IReadOnlyList<WordExample> Examples,
    [property: JsonPropertyName("relations")] IReadOnlyList<WordRelation> Relations);

public sealed record WordExample(
    [property: JsonPropertyName("example_id")] uint ExampleId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("example_en")] string ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi,
    [property: JsonPropertyName("order")] int Order);

public sealed record WordRelation(
    [property: JsonPropertyName("relation_id")] uint RelationId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("relation_type")] string RelationType,
    [property: JsonPropertyName("related_word")] string RelatedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("is_quiz_eligible")] bool IsQuizEligible);

public sealed record WordAudio(
    [property: JsonPropertyName("audio_id")] uint AudioId,
    [property: JsonPropertyName("accent")] string Accent,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("status")] string Status);
