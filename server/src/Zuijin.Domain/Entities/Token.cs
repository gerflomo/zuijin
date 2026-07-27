using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class Token : BaseEntity<long>
{
    public string Hash { get; set; } = string.Empty;
    public TokenType Type { get; set; }
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public long? ParentTokenId { get; set; }

    /// <summary>
    /// The authorization code this token descends from, so replaying that code can revoke
    /// everything it produced (RFC 6749 section 4.1.2).
    /// </summary>
    public long? AuthorizationGrantId { get; set; }

    public Client Client { get; set; } = null!;
    public User? User { get; set; }
    public Token? ParentToken { get; set; }
}
