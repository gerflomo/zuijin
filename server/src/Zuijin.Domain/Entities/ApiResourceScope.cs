namespace Zuijin.Domain.Entities;

/// <summary>
/// Associates a scope with the API resource that owns it. Requesting the scope is what
/// puts the resource into the token's audience.
/// </summary>
public class ApiResourceScope : BaseEntity<long>
{
    public Guid ApiResourceId { get; set; }
    public Guid ScopeId { get; set; }

    public ApiResource ApiResource { get; set; } = null!;
    public Scope Scope { get; set; } = null!;
}
