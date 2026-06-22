using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.DTOs;

public sealed record KnnRebuildStatusDto(
    [property: JsonPropertyName("last_rebuild_at")] DateTime? LastRebuildAt,
    [property: JsonPropertyName("is_running")] bool IsRunning);
