namespace Zuijin.Domain.Entities;

public class User : BaseEntity<Guid>
{
    public string SubjectId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FailedLoginCount { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<UserClaim> Claims { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
