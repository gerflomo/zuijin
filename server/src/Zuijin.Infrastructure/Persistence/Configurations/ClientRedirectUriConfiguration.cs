using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ClientRedirectUriConfiguration : BaseEntityConfiguration<ClientRedirectUri, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClientRedirectUri> builder)
    {
        builder.ToTable("ClientRedirectUris");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.Property(r => r.Uri).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Type).IsRequired();
    }
}
