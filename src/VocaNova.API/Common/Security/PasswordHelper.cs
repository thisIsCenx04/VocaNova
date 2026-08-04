namespace VocaNova.API.Common.Security;

public static class PasswordHelper
{
    public const int WorkFactor = 12;

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        return global::BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (global::BCrypt.Net.BcryptAuthenticationException)
        {
            return false;
        }
        catch (global::BCrypt.Net.HashInformationException)
        {
            return false;
        }
        catch (global::BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
