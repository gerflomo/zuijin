namespace Zuijin.Domain.Entities;

public class ClientScope : BaseEntity<long>
{
    public Guid ClientId { get; set; }
    public Guid ScopeId { get; set; }

    public Client Client { get; set; } = null!;
    public Scope Scope { get; set; } = null!;
}
