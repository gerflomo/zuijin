using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Features.Users;

/// <summary>
/// Builds the claim sets that go into issued tokens: RBAC claims for the access token and
/// scope-gated profile claims for the ID token.
/// </summary>
public sealed class UserClaimsResolver
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public UserClaimsResolver(
        IScopeRepository scopeRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _scopeRepository = scopeRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// Roles and permissions resolved at issuance time, so a token reflects the access the
    /// user had when it was minted.
    /// </summary>
    public async Task<Dictionary<string, object>> ResolveAuthorizationClaims(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var claims = new Dictionary<string, object>();

        var roles = await _roleRepository.GetByUserId(userId, cancellationToken);
        if (roles.Count > 0)
        {
            claims[StandardClaimTypes.Role] = roles.Select(role => role.Name).ToArray();
        }

        var permissions = await _permissionRepository.GetByUserId(userId, cancellationToken);
        if (permissions.Count > 0)
        {
            claims[StandardClaimTypes.Permission] = permissions
                .Select(permission => permission.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        return claims;
    }

    /// <summary>
    /// Profile claims the granted scopes entitle the client to see. A claim the scopes do
    /// not cover is never emitted, even when the user has a value for it.
    /// </summary>
    public async Task<Dictionary<string, object>> ResolveIdentityClaims(
        User user,
        IReadOnlyList<string> grantedScopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new Dictionary<string, object>();
        var allowedClaimTypes = await GetClaimTypesForScopes(grantedScopes, cancellationToken);

        if (allowedClaimTypes.Count == 0)
        {
            return claims;
        }

        AddIfAllowed(claims, allowedClaimTypes, StandardClaimTypes.PreferredUsername, user.Username);
        AddIfAllowed(claims, allowedClaimTypes, StandardClaimTypes.Email, user.Email);
        AddIfAllowed(claims, allowedClaimTypes, StandardClaimTypes.EmailVerified, user.EmailConfirmed);

        // Stored claims win over the built-ins so an administrator can override them.
        foreach (var userClaim in user.Claims)
        {
            if (allowedClaimTypes.Contains(userClaim.ClaimType))
            {
                claims[userClaim.ClaimType] = userClaim.ClaimValue;
            }
        }

        return claims;
    }

    private async Task<HashSet<string>> GetClaimTypesForScopes(
        IReadOnlyList<string> grantedScopes,
        CancellationToken cancellationToken)
    {
        if (grantedScopes.Count == 0)
        {
            return [];
        }

        var scopes = await _scopeRepository.GetByNames(grantedScopes, cancellationToken);

        return scopes
            .SelectMany(scope => scope.Claims)
            .Select(claim => claim.ClaimType)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void AddIfAllowed(
        Dictionary<string, object> claims,
        HashSet<string> allowedClaimTypes,
        string claimType,
        object? value)
    {
        if (value is not null && allowedClaimTypes.Contains(claimType))
        {
            claims[claimType] = value;
        }
    }
}
