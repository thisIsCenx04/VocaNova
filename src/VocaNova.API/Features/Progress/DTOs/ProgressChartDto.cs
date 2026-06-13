using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.DTOs;

public sealed record ProgressChartDto(
    [property: JsonPropertyName("granularity")] string Granularity,
    [property: JsonPropertyName("points")] IReadOnlyCollection<ProgressChartPointDto> Points);
