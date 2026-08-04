using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordRelationDto(
    [property: JsonPropertyName("relation_id")] uint RelationId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("relation_type")] string RelationType,
    [property: JsonPropertyName("related_word")] string RelatedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("is_quiz_eligible")] bool IsQuizEligible);
