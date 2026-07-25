using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class SigningKeyRepository : ISigningKeyRepository
{
    private readonly ZuijinDbContext _context;

    public SigningKeyRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<SigningKey?> GetActiveKey(CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.IsActive, cancellationToken);
    }

    public async Task<SigningKey?> GetByKeyId(string keyId, CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyId == keyId, cancellationToken);
    }

    public async Task<IReadOnlyList<SigningKey>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys
            .AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(SigningKey key, CancellationToken cancellationToken = default)
    {
        await _context.SigningKeys.AddAsync(key, cancellationToken);
    }

    public Task Update(SigningKey key, CancellationToken cancellationToken = default)
    {
        _context.SigningKeys.Update(key);
        return Task.CompletedTask;
    }
}
