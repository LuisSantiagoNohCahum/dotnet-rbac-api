namespace Example.Rbac.Api.Interfaces;

public interface ISessionWrapper
{
    int UserId { get; }
    string? UserName { get; }
    string? Email { get; }
}
