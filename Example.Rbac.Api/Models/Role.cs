namespace Example.Rbac.Api.Models;

public class Role: Entity
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// To avoid delete the default roles.
    /// </summary>
    public bool IsDefault { get; set; }
    public List<Permission> Permissions { get; set; } = [];
}
