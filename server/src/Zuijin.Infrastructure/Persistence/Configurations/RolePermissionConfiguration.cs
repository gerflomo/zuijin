using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : BaseEntityConfiguration<RolePermission, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Id).UseIdentityColumn();

        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
    }
}
