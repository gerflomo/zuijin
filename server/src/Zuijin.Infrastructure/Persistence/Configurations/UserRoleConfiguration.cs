using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : BaseEntityConfiguration<UserRole, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).UseIdentityColumn();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
    }
}
