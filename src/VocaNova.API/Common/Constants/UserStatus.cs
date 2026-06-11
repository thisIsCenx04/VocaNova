namespace VocaNova.API.Common.Constants;

public static class UserStatus
{
    public const string Active = "active";
    public const string Locked = "locked";
    public const string Deleted = "deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Active,
        Locked,
        Deleted,
    };
}
