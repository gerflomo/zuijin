using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class AuthorizationGrantConfiguration : BaseEntityConfiguration<AuthorizationGrant, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuthorizationGrant> builder)
    {
        builder.ToTable("AuthorizationGrants");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).UseIdentityColumn();

        builder.Property(g => g.CodeHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(g => g.CodeHash);

        builder.Property(g => g.RedirectUri).HasMaxLength(2000).IsRequired();
        builder.Property(g => g.Scopes).HasMaxLength(2000).IsRequired();
        builder.Property(g => g.CodeChallenge).HasMaxLength(500);
        builder.Property(g => g.CodeChallengeMethod).HasMaxLength(10);
        builder.Property(g => g.Nonce).HasMaxLength(500);
        builder.Property(g => g.IsUsed).HasDefaultValue(false);
        builder.Property(g => g.ExpiresAt).IsRequired();

        builder.HasOne(g => g.Client).WithMany().HasForeignKey(g => g.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(g => g.User).WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
