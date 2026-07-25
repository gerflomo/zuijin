using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : BaseEntityConfiguration<Permission, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.GroupName).HasMaxLength(100);
        builder.Property(p => p.IsSystem).HasDefaultValue(false);
    }
}
