using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ApiResourceScopeConfiguration : BaseEntityConfiguration<ApiResourceScope, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ApiResourceScope> builder)
    {
        builder.ToTable("ApiResourceScopes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.HasIndex(s => new { s.ApiResourceId, s.ScopeId }).IsUnique();

        builder.HasOne(s => s.Scope)
            .WithMany()
            .HasForeignKey(s => s.ScopeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
