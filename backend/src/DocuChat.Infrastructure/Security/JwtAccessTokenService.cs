using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocuChat.Application;
using DocuChat.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DocuChat.Infrastructure.Security;

public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public string Create(User user)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email)],
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
