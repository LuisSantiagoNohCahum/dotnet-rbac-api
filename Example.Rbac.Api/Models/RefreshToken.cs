namespace Example.Rbac.Api.Models;

public class RefreshToken: Entity
{
    public int UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
    public bool IsRevoked { get; set; }
}
