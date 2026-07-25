namespace Zuijin.Domain.Entities;

public class ScopeClaim : BaseEntity<long>
{
    public Guid ScopeId { get; set; }
    public string ClaimType { get; set; } = string.Empty;

    public Scope Scope { get; set; } = null!;
}
