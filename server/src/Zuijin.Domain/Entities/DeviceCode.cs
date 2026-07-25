using Zuijin.Domain.Constants;
using Zuijin.Domain.Enums;

namespace Zuijin.Domain.Entities;

public class DeviceCode : BaseEntity<long>
{
    public string DeviceCodeValue { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DeviceCodeStatus Status { get; set; } = DeviceCodeStatus.Pending;
    public int PollingInterval { get; set; } = TokenDefaults.DeviceCodePollingIntervalSeconds;
    public DateTimeOffset ExpiresAt { get; set; }

    public Client Client { get; set; } = null!;
    public User? User { get; set; }
}
