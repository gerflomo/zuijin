namespace Zuijin.Domain.Entities;

public class Consent : BaseEntity<long>
{
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }

    public User User { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
