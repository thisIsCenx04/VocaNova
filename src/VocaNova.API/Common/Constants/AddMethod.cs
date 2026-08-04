namespace VocaNova.API.Common.Constants;

public static class AddMethod
{
    public const string Manual = "manual";
    public const string Search = "search";
    public const string RandomTopic = "random_topic";
    public const string RandomSynonym = "random_synonym";
    public const string RandomAntonym = "random_antonym";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Manual,
        Search,
        RandomTopic,
        RandomSynonym,
        RandomAntonym,
    };
}
