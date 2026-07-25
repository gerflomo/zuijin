using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class Token : BaseEntity<long>
{
    public string TokenHash { get; set; } = string.Empty;
    public TokenType TokenType { get; set; }
    public Guid ClientId { get; set; }
    public Guid? UserId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public long? ParentTokenId { get; set; }

    public Client Client { get; set; } = null!;
    public User? User { get; set; }
    public Token? ParentToken { get; set; }
}
