using CR.Model.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CR.Model.TypeConfiguration.Users
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ConfigureBaseEntity();
            builder
                .HasMany(e => e.Roles)
                .WithMany(e => e.Users)
                .UsingEntity<UserRoleMap>(
                    j =>
                        j.HasOne(clm => clm.Role)
                            .WithMany(l => l.UserRoleMaps)
                            .HasForeignKey(clm => clm.RoleId)
                            .OnDelete(DeleteBehavior.Cascade),
                    j =>
                        j.HasOne(clm => clm.User)
                            .WithMany(c => c.UserRoleMaps)
                            .HasForeignKey(clm => clm.UserId)
                            .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        // Specify composite key on the join entity
                        j.HasKey(clm => new { clm.UserId, clm.RoleId });
                        j.HasIndex(x => x.UserId);
                        j.HasIndex(x => x.RoleId);
                    }
                );
        }
    }
}

