namespace VocaNova.API.Common.Constants;

public static class TopicPreferenceSource
{
    public const string KnnSuggested = "knn_suggested";
    public const string UserSelected = "user_selected";
    public const string Onboarding = "onboarding";

    public static readonly IReadOnlySet<string> NeighborSources = new HashSet<string>(StringComparer.Ordinal)
    {
        UserSelected,
        Onboarding,
    };
}
