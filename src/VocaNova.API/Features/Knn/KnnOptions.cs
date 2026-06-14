namespace VocaNova.API.Features.Knn;

public sealed class KnnOptions
{
    public const string SectionName = "Knn";

    public KnnOnboardingOptions Onboarding { get; set; } = new();

    public KnnLearningOptions Learning { get; set; } = new();
}

public sealed class KnnOnboardingOptions
{
    public int KValue { get; set; } = 5;

    public int DefaultTopicLimit { get; set; } = 10;

    public double MinSimilarity { get; set; } = 0.1;

    public int CacheTtlMinutes { get; set; } = 30;
}

public sealed class KnnLearningOptions
{
    public int KValue { get; set; } = 5;

    public int MinSessions { get; set; } = 5;

    public double MinSimilarity { get; set; } = 0.1;

    public int RecommendationCount { get; set; } = 50;

    public int RebuildIntervalHours { get; set; } = 24;

    public int CacheTtlMinutes { get; set; } = 60;
}
