namespace Zuijin.Domain.Entities;

public class AuthorizationGrant : BaseEntity<long>
{
    /// <summary>
    /// SHA-256 hash of the authorization code. The plaintext code is returned to the
    /// client once and never persisted, so a database leak cannot be replayed.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Nonce { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public Client Client { get; set; } = null!;
    public User User { get; set; } = null!;
}
