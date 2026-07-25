using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Configurations;

public class ClientScopeConfiguration : BaseEntityConfiguration<ClientScope, long>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClientScope> builder)
    {
        builder.ToTable("ClientScopes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.HasOne(s => s.Scope).WithMany(sc => sc.ClientScopes).HasForeignKey(s => s.ScopeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ClientId, s.ScopeId }).IsUnique();
    }
}
