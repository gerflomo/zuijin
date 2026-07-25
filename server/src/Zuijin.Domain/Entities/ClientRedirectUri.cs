using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class ClientRedirectUri : BaseEntity<long>
{
    public Guid ClientId { get; set; }
    public string Uri { get; set; } = string.Empty;
    public RedirectUriType Type { get; set; } = RedirectUriType.Redirect;

    public Client Client { get; set; } = null!;
}
