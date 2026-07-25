namespace Zuijin.Domain.Entities;

public class AuthorizationGrant : BaseEntity<long>
{
    public string AuthorizationCode { get; set; } = string.Empty;
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
