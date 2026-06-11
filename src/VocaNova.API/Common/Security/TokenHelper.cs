using System.Security.Cryptography;
using System.Text;

namespace VocaNova.API.Common.Security;

public static class TokenHelper
{
    public static string HashSha256(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Token is required.", nameof(rawToken));
        }

        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
