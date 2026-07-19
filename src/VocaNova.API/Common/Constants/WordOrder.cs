namespace VocaNova.API.Common.Constants;

public static class WordOrder
{
    public const string Newest = "newest";
    public const string Oldest = "oldest";
    public const string Random = "random";
    public const string ByDifficulty = "by_difficulty";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Newest,
        Oldest,
        Random,
        ByDifficulty,
    };
}
