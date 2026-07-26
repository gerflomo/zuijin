using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : BaseEntityConfiguration<Role, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        // Filtered so a soft-deleted role does not reserve its name forever.
        builder.HasIndex(r => r.Name).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.IsSystem).HasDefaultValue(false);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasMany(r => r.RolePermissions).WithOne(rp => rp.Role).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
