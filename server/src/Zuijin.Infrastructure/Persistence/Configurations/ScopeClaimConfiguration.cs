using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ScopeClaimConfiguration : BaseEntityConfiguration<ScopeClaim, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScopeClaim> builder)
    {
        builder.ToTable("ScopeClaims");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.ClaimType).HasMaxLength(200).IsRequired();

        builder.HasIndex(c => new { c.ScopeId, c.ClaimType }).IsUnique();
    }
}
