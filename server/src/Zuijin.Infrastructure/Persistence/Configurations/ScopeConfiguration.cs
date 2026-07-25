using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ScopeConfiguration : BaseEntityConfiguration<Scope, Guid>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("Scopes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.IsStandard).HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasMany(s => s.Claims).WithOne(c => c.Scope).HasForeignKey(c => c.ScopeId).OnDelete(DeleteBehavior.Cascade);
    }
}
