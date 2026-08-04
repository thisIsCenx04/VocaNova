namespace VocaNova.API.Common.Constants;

public static class CefrLevel
{
    public const string A1 = "A1";
    public const string A2 = "A2";
    public const string B1 = "B1";
    public const string B2 = "B2";
    public const string C1 = "C1";
    public const string C2 = "C2";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        A1,
        A2,
        B1,
        B2,
        C1,
        C2,
    };
}
