namespace Example.Rbac.Api.Models;

public class RoleAssignments: Entity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool UseDateRange { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}
