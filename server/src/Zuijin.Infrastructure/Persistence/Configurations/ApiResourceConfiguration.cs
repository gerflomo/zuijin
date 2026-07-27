using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ApiResourceConfiguration : BaseEntityConfiguration<ApiResource, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ApiResource> builder)
    {
        builder.ToTable("ApiResources");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        // Filtered so a soft-deleted resource does not reserve its name forever.
        builder.HasIndex(r => r.Name).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(r => r.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasMany(r => r.Scopes)
            .WithOne(s => s.ApiResource)
            .HasForeignKey(s => s.ApiResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
