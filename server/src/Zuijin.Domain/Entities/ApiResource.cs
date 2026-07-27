namespace Zuijin.Domain.Entities;

/// <summary>
/// A protected API that access tokens can be issued for.
/// <see cref="Name"/> is the value published in the token's audience claim, so a resource
/// server can reject tokens that were minted for a different API.
/// </summary>
public class ApiResource : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ApiResourceScope> Scopes { get; set; } = [];
}
