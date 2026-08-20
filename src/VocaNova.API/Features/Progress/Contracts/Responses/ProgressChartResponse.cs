using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.Contracts.Responses;

public sealed record ProgressChartResponse(
    [property: JsonPropertyName("granularity")] string Granularity,
    [property: JsonPropertyName("points")] IReadOnlyCollection<ProgressChartPointResponse> Points);

public sealed record ProgressChartPointResponse(
    [property: JsonPropertyName("period_start")] DateOnly PeriodStart,
    [property: JsonPropertyName("period_end")] DateOnly PeriodEnd,
    [property: JsonPropertyName("period_label")] string PeriodLabel,
    [property: JsonPropertyName("sessions_count")] int SessionsCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("total_answers")] int TotalAnswers,
    [property: JsonPropertyName("accuracy")] float Accuracy);
