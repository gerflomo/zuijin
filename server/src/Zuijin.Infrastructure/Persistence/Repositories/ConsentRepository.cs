using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class ConsentRepository : IConsentRepository
{
    private readonly ZuijinDbContext _context;

    public ConsentRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<Consent?> GetByUserAndClient(Guid userId, Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Consents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ClientId == clientId, cancellationToken);
    }

    public async Task Add(Consent consent, CancellationToken cancellationToken = default)
    {
        await _context.Consents.AddAsync(consent, cancellationToken);
    }

    public Task Update(Consent consent, CancellationToken cancellationToken = default)
    {
        _context.Consents.Update(consent);
        return Task.CompletedTask;
    }

    public Task Delete(Consent consent, CancellationToken cancellationToken = default)
    {
        _context.Consents.Remove(consent);
        return Task.CompletedTask;
    }

    public async Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await _context.Consents
            .Where(c => c.ExpiresAt != null && c.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
