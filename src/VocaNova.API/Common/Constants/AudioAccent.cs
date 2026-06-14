namespace VocaNova.API.Common.Constants;

public static class AudioAccent
{
    public const string Uk = "uk";
    public const string Us = "us";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Uk,
        Us,
    };
}
