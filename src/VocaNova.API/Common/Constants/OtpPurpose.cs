namespace VocaNova.API.Common.Constants;

public static class OtpPurpose
{
    public const string Register = "register";
    public const string Verify = "verify";
    public const string Reset = "reset";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Register,
        Verify,
        Reset,
    };
}
