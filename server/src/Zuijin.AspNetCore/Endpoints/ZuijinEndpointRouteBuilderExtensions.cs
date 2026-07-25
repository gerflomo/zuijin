using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Zuijin.AspNetCore.Endpoints;

public static class ZuijinEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapZuijinEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // OAuth/OIDC endpoints will be mapped here in subsequent phases:
        // - /.well-known/openid-configuration (Phase 2)
        // - /.well-known/jwks (Phase 2)
        // - /connect/token (Phase 3)
        // - /connect/authorize (Phase 4)
        // - /connect/userinfo (Phase 5)
        // - /connect/revoke (Phase 5)
        // - /connect/introspect (Phase 5)
        // - /connect/deviceauthorize (Phase 6)

        return endpoints;
    }

    public static IEndpointRouteBuilder MapZuijinAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Admin API endpoints will be mapped here in Phase 8:
        // - /api/admin/clients
        // - /api/admin/users
        // - /api/admin/scopes
        // - /api/admin/roles
        // - /api/admin/permissions

        return endpoints;
    }
}
