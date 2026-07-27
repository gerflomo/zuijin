using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Zuijin.Application.Abstractions;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence;

public class ZuijinDbContext : DbContext, IUnitOfWork
{
    private readonly IClock _clock;

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientRedirectUri> ClientRedirectUris => Set<ClientRedirectUri>();
    public DbSet<ClientGrantType> ClientGrantTypes => Set<ClientGrantType>();
    public DbSet<ClientScope> ClientScopes => Set<ClientScope>();
    public DbSet<Scope> Scopes => Set<Scope>();
    public DbSet<ScopeClaim> ScopeClaims => Set<ScopeClaim>();
    public DbSet<ApiResource> ApiResources => Set<ApiResource>();
    public DbSet<ApiResourceScope> ApiResourceScopes => Set<ApiResourceScope>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuthorizationGrant> AuthorizationGrants => Set<AuthorizationGrant>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<SigningKey> SigningKeys => Set<SigningKey>();
    public DbSet<DeviceCode> DeviceCodes => Set<DeviceCode>();
    public DbSet<Consent> Consents => Set<Consent>();

    public ZuijinDbContext(DbContextOptions<ZuijinDbContext> options, IClock clock) : base(options)
    {
        _clock = clock;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZuijinDbContext).Assembly);
    }

    Task<int> IUnitOfWork.SaveChanges(CancellationToken cancellationToken)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetPropertyIfExists(entry, nameof(BaseEntity<object>.CreatedAt), _clock.UtcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                SetPropertyIfExists(entry, nameof(BaseEntity<object>.UpdatedAt), _clock.UtcNow);
            }
        }
    }

    private static void SetPropertyIfExists(EntityEntry entry, string propertyName, object value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property != null)
        {
            property.CurrentValue = value;
        }
    }
}
