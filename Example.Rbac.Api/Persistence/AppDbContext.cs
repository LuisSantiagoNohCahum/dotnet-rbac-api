namespace Example.Rbac.Api.Persistence;

using BCrypt.Net;
using Mapster;

public class AppDbContext(DbContextOptions<AppDbContext> options, ISessionWrapper sessionWrapper): DbContext(options)
{
    private readonly ISessionWrapper _sessionWrapper = sessionWrapper;

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(250);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity<RoleAssignments>();

        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(250);

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Key)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Key)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Permissions)
            .WithMany()
            .UsingEntity(j => 
            {
                // Defines shadow table name with custom primary key (avoid use composite key).
                j.ToTable("RolePermissions");
                j.IndexerProperty<int>("Id");
                j.HasKey("Id");
            });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.CreatedBy = _sessionWrapper.UserName ?? "System";
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = now;
                entity.UpdatedBy = _sessionWrapper.UserName ?? "System";
            }
        }
    }   
}

