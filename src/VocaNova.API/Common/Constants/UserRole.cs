namespace VocaNova.API.Common.Constants;

public static class UserRole
{
    public const string Admin = "admin";
    public const string SuperAdmin = "super_admin";
    public const string User = "user";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Admin,
        SuperAdmin,
        User,
    };
}
