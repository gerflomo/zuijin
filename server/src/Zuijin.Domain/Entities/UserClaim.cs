namespace Zuijin.Domain.Entities;

public class UserClaim : BaseEntity<long>
{
    public Guid UserId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
