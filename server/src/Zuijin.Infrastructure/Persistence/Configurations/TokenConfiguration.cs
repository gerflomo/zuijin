using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class TokenConfiguration : BaseEntityConfiguration<Token, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("Tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        builder.Property(t => t.TokenHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(t => t.TokenHash);

        builder.Property(t => t.TokenType).IsRequired();
        builder.Property(t => t.Scopes).HasMaxLength(2000).IsRequired();
        builder.Property(t => t.IsRevoked).HasDefaultValue(false);
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.HasIndex(t => t.ExpiresAt);

        builder.HasOne(t => t.Client).WithMany().HasForeignKey(t => t.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.ParentToken).WithMany().HasForeignKey(t => t.ParentTokenId).OnDelete(DeleteBehavior.NoAction);
    }
}
