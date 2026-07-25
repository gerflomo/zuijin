namespace Zuijin.Domain.Entities;

public class SigningKey : BaseEntity<long>
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RS256";
    public byte[] KeyData { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTimeOffset ActivatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
