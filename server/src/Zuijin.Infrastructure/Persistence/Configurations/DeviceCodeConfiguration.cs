using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class DeviceCodeConfiguration : BaseEntityConfiguration<DeviceCode, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DeviceCode> builder)
    {
        builder.ToTable("DeviceCodes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseIdentityColumn();

        builder.Property(d => d.DeviceCodeHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(d => d.DeviceCodeHash);

        builder.Property(d => d.UserCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(d => d.UserCode);

        builder.Property(d => d.Scopes).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.Status).HasDefaultValue(DeviceCodeStatus.Pending);
        builder.Property(d => d.PollingInterval).HasDefaultValue(5);
        builder.Property(d => d.ExpiresAt).IsRequired();

        builder.HasOne(d => d.Client).WithMany().HasForeignKey(d => d.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
