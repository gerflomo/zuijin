using Zuijin.Domain.Constants;
using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class Client : BaseEntity<Guid>
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? SecretHash { get; set; }
    public ClientType Type { get; set; } = ClientType.Confidential;
    public bool RequirePkce { get; set; } = true;
    public bool RequireConsent { get; set; } = true;
    public bool AllowOfflineAccess { get; set; }
    public int AccessTokenLifetime { get; set; } = TokenDefaults.AccessTokenLifetimeSeconds;
    public int RefreshTokenLifetime { get; set; } = TokenDefaults.RefreshTokenLifetimeSeconds;
    public int IdTokenLifetime { get; set; } = TokenDefaults.IdTokenLifetimeSeconds;
    public bool IsActive { get; set; } = true;

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ClientRedirectUri> RedirectUris { get; set; } = [];
    public ICollection<ClientGrantType> GrantTypes { get; set; } = [];
    public ICollection<ClientScope> Scopes { get; set; } = [];
}
