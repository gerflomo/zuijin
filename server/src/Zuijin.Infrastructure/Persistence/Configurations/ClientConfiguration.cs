using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : BaseEntityConfiguration<Client, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClientId).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.ClientId).IsUnique();

        builder.Property(c => c.ClientName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ClientSecretHash).HasMaxLength(500);
        builder.Property(c => c.ClientType).IsRequired();
        builder.Property(c => c.RequirePkce).HasDefaultValue(true);
        builder.Property(c => c.RequireConsent).HasDefaultValue(true);
        builder.Property(c => c.AllowOfflineAccess).HasDefaultValue(false);
        builder.Property(c => c.AccessTokenLifetime).HasDefaultValue(3600);
        builder.Property(c => c.RefreshTokenLifetime).HasDefaultValue(2592000);
        builder.Property(c => c.IdTokenLifetime).HasDefaultValue(3600);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasMany(c => c.RedirectUris).WithOne(r => r.Client).HasForeignKey(r => r.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.GrantTypes).WithOne(g => g.Client).HasForeignKey(g => g.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Scopes).WithOne(s => s.Client).HasForeignKey(s => s.ClientId).OnDelete(DeleteBehavior.Cascade);
    }
}
