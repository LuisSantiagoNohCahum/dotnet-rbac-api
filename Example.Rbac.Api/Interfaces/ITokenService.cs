namespace Example.Rbac.Api.Interfaces;

public interface ITokenService
{
    string GenerateRefreshToken();
    string GenerateToken(TokenService.GenerateTokenDto options);
}
