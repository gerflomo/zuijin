using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class SigningKeyConfiguration : BaseEntityConfiguration<SigningKey, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SigningKey> builder)
    {
        builder.ToTable("SigningKeys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).UseIdentityColumn();

        builder.Property(k => k.KeyId).HasMaxLength(100).IsRequired();
        builder.HasIndex(k => k.KeyId).IsUnique();

        builder.Property(k => k.Algorithm).HasMaxLength(20).IsRequired();
        builder.Property(k => k.KeyData).IsRequired();
        builder.Property(k => k.IsActive).HasDefaultValue(false);
        builder.Property(k => k.ActivatedAt).IsRequired();
    }
}
