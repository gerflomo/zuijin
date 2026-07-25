using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class UserConfiguration : BaseEntityConfiguration<User, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.SubjectId).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.SubjectId).IsUnique();

        builder.Property(u => u.Username).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email);

        builder.Property(u => u.EmailConfirmed).HasDefaultValue(false);
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsLockedOut).HasDefaultValue(false);
        builder.Property(u => u.FailedLoginCount).HasDefaultValue(0);
        builder.Property(u => u.MfaEnabled).HasDefaultValue(false);
        builder.Property(u => u.MfaSecret).HasMaxLength(500);

        builder.HasMany(u => u.Claims).WithOne(c => c.User).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
