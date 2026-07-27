using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ZuijinDbContext _context;

    public RoleRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        // Filtering Roles by a subquery keeps Include valid: applying it after a Select that
        // projects through a navigation is something EF Core cannot translate.
        var roleIds = _context.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.RoleId);

        return await _context.Roles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .Include(role => role.RolePermissions).ThenInclude(rolePermission => rolePermission.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCount(CancellationToken cancellationToken = default)
    {
        return await _context.Roles.CountAsync(cancellationToken);
    }

    public async Task Add(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public Task Update(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        return Task.CompletedTask;
    }

    public Task Delete(Role role, CancellationToken cancellationToken = default)
    {
        role.IsDeleted = true;
        _context.Roles.Update(role);
        return Task.CompletedTask;
    }
}
