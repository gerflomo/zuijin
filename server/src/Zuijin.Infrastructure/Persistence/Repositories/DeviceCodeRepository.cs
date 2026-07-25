using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class DeviceCodeRepository : IDeviceCodeRepository
{
    private readonly ZuijinDbContext _context;

    public DeviceCodeRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceCode?> GetByDeviceCodeHash(string deviceCodeHash, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceCodes
            .AsNoTracking()
            .Include(d => d.Client)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DeviceCodeValue == deviceCodeHash, cancellationToken);
    }

    public async Task<DeviceCode?> GetByUserCode(string userCode, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceCodes
            .AsNoTracking()
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.UserCode == userCode, cancellationToken);
    }

    public async Task Add(DeviceCode deviceCode, CancellationToken cancellationToken = default)
    {
        await _context.DeviceCodes.AddAsync(deviceCode, cancellationToken);
    }

    public Task Update(DeviceCode deviceCode, CancellationToken cancellationToken = default)
    {
        _context.DeviceCodes.Update(deviceCode);
        return Task.CompletedTask;
    }

    public async Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await _context.DeviceCodes
            .Where(d => d.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
