using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Zuijin.Application.Features.Tokens;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Errors;

namespace Zuijin.AspNetCore.Endpoints.Token;

/// <summary>
/// The OAuth 2.0 token endpoint. Grant types are dispatched from the request body.
/// </summary>
public static class TokenEndpoints
{
    private const string GrantTypeParameter = "grant_type";
    private const string ScopeParameter = "scope";

    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(ZuijinEndpointPaths.Token, IssueToken)
            .AddEndpointFilter<OAuthErrorEndpointFilter>()
            .WithName("Token");

        return endpoints;
    }

    private static async Task<IResult> IssueToken(
        HttpContext httpContext,
        ClientCredentialsTokenHandler clientCredentialsHandler,
        AuthorizationCodeTokenHandler authorizationCodeHandler,
        RefreshTokenGrantHandler refreshTokenHandler,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            throw new OAuthException(OAuthError.InvalidRequest(
                "The token endpoint expects an application/x-www-form-urlencoded body."));
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var grantType = form[GrantTypeParameter].ToString();

        if (string.IsNullOrEmpty(grantType))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The grant_type parameter is required."));
        }

        var (clientId, clientSecret) = ClientCredentialsReader.Read(httpContext.Request, form);

        var result = grantType switch
        {
            GrantTypes.ClientCredentials => await clientCredentialsHandler.Handle(new ClientCredentialsTokenRequest
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RequestedScopes = ParseScopes(form[ScopeParameter].ToString())
            }, cancellationToken),

            GrantTypes.AuthorizationCode => await authorizationCodeHandler.Handle(new AuthorizationCodeTokenRequest
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Code = form["code"].ToString(),
                RedirectUri = form["redirect_uri"].ToString(),
                CodeVerifier = form["code_verifier"].ToString()
            }, cancellationToken),

            GrantTypes.RefreshToken => await refreshTokenHandler.Handle(new RefreshTokenGrantRequest
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RefreshToken = form["refresh_token"].ToString(),
                RequestedScopes = ParseScopes(form[ScopeParameter].ToString())
            }, cancellationToken),

            _ => throw new OAuthException(OAuthError.UnsupportedGrantType(
                $"The grant type '{grantType}' is not supported."))
        };

        // RFC 6749 section 5.1: token responses must never be cached.
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";

        return Results.Json(new TokenResponseDocument
        {
            AccessToken = result.AccessToken,
            TokenType = result.TokenType,
            ExpiresIn = result.ExpiresIn,
            Scope = result.Scope,
            IdToken = result.IdToken,
            RefreshToken = result.RefreshToken
        });
    }

    private static IReadOnlyList<string> ParseScopes(string scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
