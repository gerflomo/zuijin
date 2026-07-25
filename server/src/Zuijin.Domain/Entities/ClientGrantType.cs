namespace Zuijin.Domain.Entities;

public class ClientGrantType : BaseEntity<long>
{
    public Guid ClientId { get; set; }
    public string GrantType { get; set; } = string.Empty;

    public Client Client { get; set; } = null!;
}
