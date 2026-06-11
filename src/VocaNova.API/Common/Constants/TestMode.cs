namespace VocaNova.API.Common.Constants;

public static class TestMode
{
    public const string Standard = "standard";
    public const string Timed = "timed";
    public const string Challenge = "challenge";
    public const string Elimination = "elimination";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Standard,
        Timed,
        Challenge,
        Elimination,
    };
}
