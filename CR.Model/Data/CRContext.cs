using System.Reflection;
using CR.Model.Roles;
using CR.Model.Users;
using ESG.Model.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CR.Model.Data;

public class CRContext(DbContextOptions<CRContext> options, IPasswordHasher<User> passwordHasher)
    : DbContext(options)
{
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("ltree");
        Assembly assemblyWithConfigurations = GetType().Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assemblyWithConfigurations);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();

            if (
                primaryKey is not null
                && primaryKey.Properties.Count == 1
                && primaryKey.Properties[0].ClrType == typeof(Guid)
            )
            {
                primaryKey.Properties[0].ValueGenerated = ValueGenerated.OnAdd;

                primaryKey
                    .Properties[0]
                    .SetValueGeneratorFactory((property, entityType) => new UuidV7ValueGenerator());
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        // UpdateTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        // UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public async Task SeedDataAsync()
    {
        await GetOrCreateUser();
    }

    public async Task<Role> GetOrCreateRole(string name)
    {
        var role = await Roles.FirstOrDefaultAsync(x => x.Name == name);

        if (role != null)
            return role;

        role = new Role { Name = name };

        Roles.Add(role);
        await SaveChangesAsync();

        return role;
    }

    public async Task GetOrCreateUser()
    {
        // Create only Admin and User roles
        var adminRole = await GetOrCreateRole("Admin");
        await GetOrCreateRole("User");

        // Default Admin credentials
        const string username = "CRAdmin";
        const string password = "CRAdmin@123";

        // Check whether the default admin already exists
        var existingUser = await Users
            .Include(u => u.UserRoleMaps)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (existingUser != null)
            return;

        // Create default Admin user
        var newUser = new User
        {
            Username = username,
            FullName = "Admin",
            Email = "admin123@gmail.com",
            Active = true,
            Password = _passwordHasher.HashPassword(new User(), password),
            UserRoleMaps = new List<UserRoleMap> { new UserRoleMap { RoleId = adminRole.RoleId } },
        };

        Users.Add(newUser);

        await SaveChangesAsync();
    }
}
