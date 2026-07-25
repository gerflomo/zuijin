using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing RFC 8628 device authorization grants.
/// </summary>
public interface IDeviceCodeRepository
{
    Task<DeviceCode?> GetByDeviceCodeHash(string deviceCodeHash, CancellationToken cancellationToken = default);
    Task<DeviceCode?> GetByUserCode(string userCode, CancellationToken cancellationToken = default);
    Task Add(DeviceCode deviceCode, CancellationToken cancellationToken = default);
    Task Update(DeviceCode deviceCode, CancellationToken cancellationToken = default);
    Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
