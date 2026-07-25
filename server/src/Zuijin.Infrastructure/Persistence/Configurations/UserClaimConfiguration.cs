using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class UserClaimConfiguration : BaseEntityConfiguration<UserClaim, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.ClaimType).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ClaimValue).HasMaxLength(2000).IsRequired();
    }
}
