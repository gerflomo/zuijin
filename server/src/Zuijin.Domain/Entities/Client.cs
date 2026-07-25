using Zuijin.Domain.Constants;
using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class Client : BaseEntity<Guid>
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; }
    public ClientType ClientType { get; set; } = ClientType.Confidential;
    public bool RequirePkce { get; set; } = true;
    public bool RequireConsent { get; set; } = true;
    public bool AllowOfflineAccess { get; set; }
    public int AccessTokenLifetime { get; set; } = TokenDefaults.AccessTokenLifetimeSeconds;
    public int RefreshTokenLifetime { get; set; } = TokenDefaults.RefreshTokenLifetimeSeconds;
    public int IdTokenLifetime { get; set; } = TokenDefaults.IdTokenLifetimeSeconds;
    public bool IsActive { get; set; } = true;

    public ICollection<ClientRedirectUri> RedirectUris { get; set; } = [];
    public ICollection<ClientGrantType> GrantTypes { get; set; } = [];
    public ICollection<ClientScope> Scopes { get; set; } = [];
}
