namespace Example.Rbac.Api.Persistence;

using BCrypt.Net;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext dbContext)
    {
        await SeedRoles(dbContext);
        await SeedPermissions(dbContext);
        await SeedAdminUser(dbContext);
    }

    private static async Task SeedRoles(AppDbContext dbContext)
    {
        if (await dbContext.Roles.AnyAsync())
            return;

        List<Role> roles = [
            new Role { Name = "Administrador", Key = "admin", Description = "Administrator role", IsDefault = true, IsActive = true, IsFullAccess = true },
            new Role { Name = "Member", Key = "user", Description = "Regular user role", IsDefault = true, IsActive = true, IsFullAccess = false }
        ];

        await dbContext.Roles.AddRangeAsync(roles);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPermissions(AppDbContext dbContext)
    {
        if (await dbContext.Permissions.AnyAsync())
            return;

        // In this case is only for this app, but if is multitenant app the appid columns should be added to allow filter by this field.

        List<Permission> permissions = [
            new Permission { Name="Ver Roles", Key = "roles.read", Description = "Read role access", Resource = "Roles", Action = "Read", IsActive = true },
            new Permission { Name="Crear Rol", Key = "roles.create", Description = "Create role access", Resource = "Roles", Action = "Create", IsActive = true },
            new Permission { Name="Eliminar Rol", Key = "roles.delete", Description = "Delete role access", Resource = "Roles", Action = "Delete", IsActive = true },
            new Permission { Name="Actualizar Rol", Key = "roles.update", Description = "Update role access information", Resource = "Roles", Action = "Update", IsActive = true },

            new Permission { Name="Ver Usuarios", Key = "users.read", Description = "Read user access", Resource = "Users", Action = "Read", IsActive = true },
            new Permission { Name="Crear Usuario", Key = "users.create", Description = "Create user access", Resource = "Users", Action = "Create", IsActive = true },
            new Permission { Name="Eliminar Usuario", Key = "users.delete", Description = "Delete user access", Resource = "Users", Action = "Delete", IsActive = true },
            new Permission { Name="Actualizar Usuario", Key = "users.update", Description = "Update user access information", Resource = "Users", Action = "Update", IsActive = true },

            new Permission { Name="Ver Permisos", Key = "permissions.read", Description = "Read permission access", Resource = "Permissions", Action = "Read", IsActive = true },
        ];

        await dbContext.Permissions.AddRangeAsync(permissions);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminUser(AppDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
            return;
      
        var adminUser = new User
        {
            UserName = "admin.default",
            Email = "admin@example.com",
            HashPassword = BCrypt.HashPassword("yP$23_0ñ"),
            EmailVerified = false,
            EnableTwoFactorAuth = false,
            IsActive = true
        };

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        var role = await dbContext.Roles.FirstAsync(r => r.Key == "admin");

        adminUser.RoleAssignments = [
            new () { 
                RoleId = role.Id, 
                UserId = adminUser.Id, 
                UseDateRange = false, 
                IsActive = true }
            ];

        await dbContext.SaveChangesAsync();
        
    }
}
