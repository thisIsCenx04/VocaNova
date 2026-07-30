using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.DTOs;

public sealed record SelectOnboardingTopicsRequest(
    [property: JsonPropertyName("topic_ids")] IReadOnlyCollection<uint>? TopicIds);
