using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record WordDetailResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("word_key")] string WordKey,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("phonetic_us")] string? PhoneticUs,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("is_phrase")] bool IsPhrase,
    [property: JsonPropertyName("senses")] IReadOnlyCollection<WordSenseResponse> Senses,
    [property: JsonPropertyName("examples")] IReadOnlyCollection<WordExampleResponse> Examples,
    [property: JsonPropertyName("relations")] IReadOnlyCollection<WordRelationResponse> Relations,
    [property: JsonPropertyName("audio")] IReadOnlyCollection<WordAudioResponse> Audio,
    [property: JsonPropertyName("derived_forms")] IReadOnlyCollection<WordDerivedFormResponse> DerivedForms,
    [property: JsonPropertyName("idioms")] IReadOnlyCollection<WordIdiomResponse> Idioms,
    [property: JsonPropertyName("topics")] IReadOnlyCollection<WordTopicResponse> Topics,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

public sealed record WordSenseResponse(
    [property: JsonPropertyName("sense_id")] uint SenseId,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("word_class")] string WordClass,
    [property: JsonPropertyName("english_definition")] string EnglishDefinition,
    [property: JsonPropertyName("vietnamese_meaning")] string? VietnameseMeaning,
    [property: JsonPropertyName("examples")] IReadOnlyCollection<WordExampleResponse> Examples,
    [property: JsonPropertyName("relations")] IReadOnlyCollection<WordRelationResponse> Relations);

public sealed record WordExampleResponse(
    [property: JsonPropertyName("example_id")] uint ExampleId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("example_en")] string ExampleEn,
    [property: JsonPropertyName("example_vi")] string? ExampleVi,
    [property: JsonPropertyName("order")] int Order);

public sealed record WordRelationResponse(
    [property: JsonPropertyName("relation_id")] uint RelationId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("relation_type")] string RelationType,
    [property: JsonPropertyName("related_word")] string RelatedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("is_quiz_eligible")] bool IsQuizEligible);

public sealed record WordDerivedFormResponse(
    [property: JsonPropertyName("derived_id")] uint DerivedId,
    [property: JsonPropertyName("derived_word")] string DerivedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("word_class")] string? WordClass);

public sealed record WordIdiomResponse(
    [property: JsonPropertyName("idiom_id")] uint IdiomId,
    [property: JsonPropertyName("idiom_text")] string IdiomText,
    [property: JsonPropertyName("meaning_en")] string? MeaningEn,
    [property: JsonPropertyName("meaning_vi")] string? MeaningVi);

public sealed record WordTopicResponse(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon);
