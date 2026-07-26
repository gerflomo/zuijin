using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly ZuijinDbContext _context;

    public TokenRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<Token?> GetByHash(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens
            .AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Hash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<Token>> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Token>> GetByClientId(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Tokens
            .AsNoTracking()
            .Where(t => t.ClientId == clientId)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(Token token, CancellationToken cancellationToken = default)
    {
        await _context.Tokens.AddAsync(token, cancellationToken);
    }

    public Task Update(Token token, CancellationToken cancellationToken = default)
    {
        _context.Tokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Tokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), cancellationToken);
    }

    public async Task RevokeByClientId(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _context.Tokens
            .Where(t => t.ClientId == clientId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), cancellationToken);
    }

    public async Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await _context.Tokens
            .Where(t => t.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
