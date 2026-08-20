using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record AgeRangeResponse(
    [property: JsonPropertyName("age_range_id")] uint AgeRangeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record RegionResponse(
    [property: JsonPropertyName("region_id")] uint RegionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId,
    [property: JsonPropertyName("parent_name")] string? ParentName,
    [property: JsonPropertyName("status")] string Status);

public sealed record OccupationResponse(
    [property: JsonPropertyName("occupation_id")] uint OccupationId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);

public sealed record EducationLevelResponse(
    [property: JsonPropertyName("education_level_id")] uint EducationLevelId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder,
    [property: JsonPropertyName("status")] string Status);

public sealed record LearningPurposeResponse(
    [property: JsonPropertyName("learning_purpose_id")] uint LearningPurposeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] string Status);
