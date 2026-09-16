using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DocuChat.Api;

public static class UserClaims
{
    public static Guid UserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Invalid user identity.");
    }
}
