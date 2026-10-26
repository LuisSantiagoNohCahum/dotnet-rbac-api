using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Example.Rbac.Api.Common;

public class TokenService(IConfiguration configuration): ITokenService
{
    public record GenerateTokenDto(int UserId, string Email, string[] Roles, string[] Permissions);

    private readonly IConfiguration _configuration = configuration;

    public string GenerateToken(GenerateTokenDto options)
    {
        var issuer = _configuration.GetValue<string>(ConfigurationConstants.JwtIssuer);
        var audience = _configuration.GetValue<string>(ConfigurationConstants.JwtAudience);
        var key = _configuration.GetValue<string>(ConfigurationConstants.JwtKey) ?? string.Empty;
        var tokenExpirationInMinutes = _configuration.GetValue<int>(ConfigurationConstants.TokenExpirationInMinutes);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, options.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, options.Email),
            new Claim(CustomClaims.UserId, options.UserId.ToString())
        };

        foreach (var role in options.Roles)
            claims.Append(new Claim(ClaimTypes.Role, role));

        foreach (var permission in options.Permissions)
            claims.Append(new Claim(CustomClaims.Permissions, permission));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var guid = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
        return Convert.ToBase64String([..randomNumber, ..guid]);
    }
}
