using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record KnnRebuildStatusResponse(
    [property: JsonPropertyName("last_rebuild_at")] DateTime? LastRebuildAt,
    [property: JsonPropertyName("is_running")] bool IsRunning);

public sealed record TriggerKnnRebuildResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("triggered_at")] DateTime TriggeredAt);
