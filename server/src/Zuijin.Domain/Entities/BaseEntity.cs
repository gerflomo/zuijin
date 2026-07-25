namespace Zuijin.Domain.Entities;

/// <summary>
/// Base class for all entities. Provides common audit and soft-delete fields.
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
