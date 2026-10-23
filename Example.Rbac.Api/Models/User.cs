namespace Example.Rbac.Api.Models;

public class User: Entity
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string HashPassword { get; set; }
    public bool EnableTwoFactorAuth { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; }
    public List<Role> Roles { get; set; } = [];
    public List<RoleAssignments> RoleAssignments { get; set; } = [];
}
