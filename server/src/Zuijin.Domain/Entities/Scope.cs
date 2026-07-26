namespace Zuijin.Domain.Entities;

public class Scope : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsStandard { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ScopeClaim> Claims { get; set; } = [];
    public ICollection<ClientScope> ClientScopes { get; set; } = [];
}
