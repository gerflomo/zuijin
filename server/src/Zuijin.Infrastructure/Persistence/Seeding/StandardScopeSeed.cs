using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;

namespace Zuijin.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seed data for the OIDC standard scopes and their claim mappings.
/// Ids and timestamps are fixed constants: EF Core migrations require stable
/// seed values to detect changes between model snapshots.
/// </summary>
public static class StandardScopeSeed
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid OpenIdId = new("019506a0-0000-7000-8000-000000000001");
    public static readonly Guid ProfileId = new("019506a0-0000-7000-8000-000000000002");
    public static readonly Guid EmailId = new("019506a0-0000-7000-8000-000000000003");
    public static readonly Guid AddressId = new("019506a0-0000-7000-8000-000000000004");
    public static readonly Guid PhoneId = new("019506a0-0000-7000-8000-000000000005");
    public static readonly Guid OfflineAccessId = new("019506a0-0000-7000-8000-000000000006");

    public static IReadOnlyList<Scope> Scopes =>
    [
        Create(OpenIdId, StandardScopes.OpenId, "OpenID", "Subject identifier (sub claim). Required for OIDC."),
        Create(ProfileId, StandardScopes.Profile, "Profile", "Basic profile information (name, picture, locale...)."),
        Create(EmailId, StandardScopes.Email, "Email", "Email address and verification status."),
        Create(AddressId, StandardScopes.Address, "Address", "Postal address."),
        Create(PhoneId, StandardScopes.Phone, "Phone", "Phone number and verification status."),
        Create(OfflineAccessId, StandardScopes.OfflineAccess, "Offline access", "Request refresh tokens for long-lived access.")
    ];

    public static IReadOnlyList<ScopeClaim> ScopeClaims
    {
        get
        {
            long id = 0;
            var claims = new List<ScopeClaim>();

            AddClaims(claims, ref id, OpenIdId, StandardClaimTypes.Subject);
            AddClaims(claims, ref id, ProfileId,
                StandardClaimTypes.Name, StandardClaimTypes.GivenName, StandardClaimTypes.FamilyName,
                StandardClaimTypes.MiddleName, StandardClaimTypes.Nickname, StandardClaimTypes.PreferredUsername,
                StandardClaimTypes.Profile, StandardClaimTypes.Picture, StandardClaimTypes.Website,
                StandardClaimTypes.Gender, StandardClaimTypes.Birthdate, StandardClaimTypes.Zoneinfo,
                StandardClaimTypes.Locale, StandardClaimTypes.UpdatedAt);
            AddClaims(claims, ref id, EmailId, StandardClaimTypes.Email, StandardClaimTypes.EmailVerified);
            AddClaims(claims, ref id, AddressId, StandardClaimTypes.Address);
            AddClaims(claims, ref id, PhoneId, StandardClaimTypes.PhoneNumber, StandardClaimTypes.PhoneNumberVerified);

            return claims;
        }
    }

    private static Scope Create(Guid id, string name, string displayName, string description)
    {
        return new Scope
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsStandard = true,
            IsActive = true,
            CreatedAt = SeedTimestamp
        };
    }

    private static void AddClaims(List<ScopeClaim> claims, ref long id, Guid scopeId, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            claims.Add(new ScopeClaim
            {
                Id = ++id,
                ScopeId = scopeId,
                ClaimType = claimType,
                CreatedAt = SeedTimestamp
            });
        }
    }
}
