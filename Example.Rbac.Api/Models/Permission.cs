namespace Example.Rbac.Api.Models;

public class Permission: Entity
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Resource { get; set; }
    public required string Action { get; set; }
}
