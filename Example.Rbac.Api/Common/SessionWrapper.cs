using System.Security.Claims;

namespace Example.Rbac.Api.Common;

public class SessionWrapper(IHttpContextAccessor context): ISessionWrapper
{
    private readonly ClaimsPrincipal? principal = context?.HttpContext?.User;

    public int UserId 
    { 
        get => principal?.FindFirst("user_id")?.Value is string userId 
            ? int.Parse(userId) : 0; 
    }

    public string? UserName 
    { 
        get => principal?.Claims
            .FirstOrDefault(c => c.Type == "user_name")?.Value; 
    }

    public string? Email 
    { 
        get => principal?.Claims
            .FirstOrDefault(c => c.Type == "email")?.Value; 
    }
}
