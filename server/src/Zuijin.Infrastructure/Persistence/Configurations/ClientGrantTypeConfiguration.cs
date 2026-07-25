using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ClientGrantTypeConfiguration : BaseEntityConfiguration<ClientGrantType, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClientGrantType> builder)
    {
        builder.ToTable("ClientGrantTypes");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).UseIdentityColumn();

        builder.Property(g => g.GrantType).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => new { g.ClientId, g.GrantType }).IsUnique();
    }
}
