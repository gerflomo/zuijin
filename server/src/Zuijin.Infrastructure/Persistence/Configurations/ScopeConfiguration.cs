using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;
using Zuijin.Infrastructure.Persistence.Seeding;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ScopeConfiguration : BaseEntityConfiguration<Scope, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("Scopes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        // Filtered so a soft-deleted scope does not reserve its name forever.
        builder.HasIndex(s => s.Name).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(s => s.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.IsStandard).HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasMany(s => s.Claims).WithOne(c => c.Scope).HasForeignKey(c => c.ScopeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasData(StandardScopeSeed.Scopes);
    }
}
