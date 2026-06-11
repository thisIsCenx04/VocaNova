using System.Security.Claims;

namespace VocaNova.API.Infrastructure.Authentication;

public static class JwtClaimsPrincipalHelper
{
    public static void AddUserIdClaimFromSubject(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (identity.HasClaim(claim => claim.Type == "user_id"))
        {
            return;
        }

        var subject = identity.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            identity.AddClaim(new Claim("user_id", subject));
        }
    }
}
