using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TNG.Users.API.Common.Configuration;
using TNG.Users.API.Domain;

namespace TNG.Users.API.Common.Services.Impl;

public class JwtTokenProvider(JwtOptions options) : ITokenProvider
{
    public string Generate(User user)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), 
            new(JwtRegisteredClaimNames.Name, user.Login)
        };
        var createdAt = DateTime.UtcNow;

        var expiresAt = createdAt.AddSeconds(options.ExpirationSeconds);

        var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);

        var symmetricSecurityKey = new SymmetricSecurityKey(keyBytes);

        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }
}