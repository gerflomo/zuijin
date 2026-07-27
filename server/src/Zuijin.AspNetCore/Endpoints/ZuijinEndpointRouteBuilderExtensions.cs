using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Zuijin.AspNetCore.Endpoints.Discovery;
using Zuijin.AspNetCore.Endpoints.Token;

namespace Zuijin.AspNetCore.Endpoints;

public static class ZuijinEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapZuijinEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDiscoveryEndpoints();
        endpoints.MapTokenEndpoints();

        // Remaining OAuth/OIDC endpoints arrive in subsequent phases:
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
