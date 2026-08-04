namespace VocaNova.API.Features.Knn;

public sealed class KnnOptions
{
    public const string SectionName = "Knn";

    public KnnOnboardingOptions Onboarding { get; set; } = new();

    public KnnLearningOptions Learning { get; set; } = new();

    public KnnVectorOptions Vector { get; set; } = new();
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

    /// <summary>
    /// Minimum mastery level a neighbour must have reached before one of their words is
    /// considered worth recommending.
    /// </summary>
    public int MinNeighborMasteryLevel { get; set; } = 3;

    /// <summary>
    /// How many words the cold-start (profile-based) path returns for a user who has not yet
    /// completed <see cref="MinSessions"/> sessions.
    /// </summary>
    public int ColdStartRecommendationCount { get; set; } = 30;
}

/// <summary>
/// Per-block weights for the hybrid profile vector. Each block is emitted as a unit-length
/// segment scaled by its weight, so a block never dominates purely because it has more
/// dimensions; relative importance is expressed only through these numbers.
/// </summary>
public sealed class KnnVectorOptions
{
    // Sign-up block.
    public double AgeRangeWeight { get; set; } = 1.0;

    public double RegionWeight { get; set; } = 0.6;

    public double OccupationWeight { get; set; } = 1.0;

    public double EducationLevelWeight { get; set; } = 0.8;

    // Onboarding block: intent is a stronger signal for what to study than demographics.
    public double LearningPurposeWeight { get; set; } = 1.5;

    public double InterestTopicsWeight { get; set; } = 2.0;
}
