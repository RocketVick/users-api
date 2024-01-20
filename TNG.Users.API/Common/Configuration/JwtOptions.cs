using Microsoft.Extensions.Options;

namespace TNG.Users.API.Common.Configuration;

public class JwtOptions
{
    public string SigningKey { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public int ExpirationSeconds { get; init; }
}