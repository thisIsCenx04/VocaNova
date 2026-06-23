using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Stats;

// JsonPropertyName tường minh vì có field chứa số (avg_accuracy_7d) — naming policy có thể đặt sai vị trí underscore.

public sealed class AdminDashboardStatsDto
{
    [JsonPropertyName("total_users")]
    public int TotalUsers { get; set; }

    [JsonPropertyName("total_words")]
    public int TotalWords { get; set; }

    [JsonPropertyName("sessions_today")]
    public int SessionsToday { get; set; }

    [JsonPropertyName("avg_accuracy_7d")]
    public double AvgAccuracy7d { get; set; }
}

public sealed class AdminAccuracyTrendPointDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("correct_count")]
    public int CorrectCount { get; set; }

    [JsonPropertyName("wrong_count")]
    public int WrongCount { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }
}

public sealed class AdminWrongWordDto
{
    [JsonPropertyName("word_id")]
    public uint WordId { get; set; }

    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("wrong_count")]
    public int WrongCount { get; set; }

    [JsonPropertyName("correct_count")]
    public int CorrectCount { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }
}

public sealed class AdminLearningStatsDto
{
    [JsonPropertyName("top_wrong_words")]
    public List<AdminWrongWordDto> TopWrongWords { get; set; } = new();

    [JsonPropertyName("accuracy_trend")]
    public List<AdminAccuracyTrendPointDto> AccuracyTrend { get; set; } = new();
}

public sealed class AdminDemographicsDto
{
    [JsonPropertyName("age_ranges")]
    public List<AdminDemographicGroupDto> AgeRanges { get; set; } = new();

    [JsonPropertyName("occupations")]
    public List<AdminDemographicGroupDto> Occupations { get; set; } = new();

    [JsonPropertyName("education_levels")]
    public List<AdminDemographicGroupDto> EducationLevels { get; set; } = new();
}

public sealed class AdminDemographicGroupDto
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("user_count")]
    public int UserCount { get; set; }
}
