using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Zuijin.Application.Features.Authorization;
using Zuijin.Application.Features.Users;
using Zuijin.AspNetCore.Authentication;
using Zuijin.AspNetCore.Endpoints.Authorize;
using Zuijin.AspNetCore.Endpoints.Ui;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;

namespace Zuijin.AspNetCore.Endpoints.Account;

/// <summary>
/// The interactive pages the authorization endpoint hands off to: signing in and consenting.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ZuijinEndpointPaths.Login, ShowLogin).WithName("Login");
        endpoints.MapPost(ZuijinEndpointPaths.Login, SignIn).WithName("SignIn");
        endpoints.MapGet(ZuijinEndpointPaths.Consent, ShowConsent).WithName("Consent");
        endpoints.MapPost(ZuijinEndpointPaths.Consent, SubmitConsent).WithName("SubmitConsent");

        return endpoints;
    }

    private static IResult ShowLogin(HttpContext httpContext)
    {
        var returnUrl = ReadReturnUrl(httpContext.Request.Query["returnUrl"]);

        return HtmlPage.Login(returnUrl, username: null, hasError: false);
    }

    private static async Task<IResult> SignIn(
        HttpContext httpContext,
        UserAuthenticator userAuthenticator,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var returnUrl = ReadReturnUrl(form["returnUrl"]);
        var username = form["username"].ToString();

        var user = await userAuthenticator.Authenticate(username, form["password"].ToString(), cancellationToken);

        if (user is null)
        {
            // Re-render rather than redirect so the failure cannot be bookmarked or replayed.
            return HtmlPage.Login(returnUrl, username, hasError: true);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ZuijinAuthenticationDefaults.SubjectClaimType, user.SubjectId)],
            ZuijinAuthenticationDefaults.SessionScheme);

        await httpContext.SignInAsync(
            ZuijinAuthenticationDefaults.SessionScheme,
            new ClaimsPrincipal(identity));

        return Results.Redirect(returnUrl);
    }

    private static async Task<IResult> ShowConsent(
        HttpContext httpContext,
        AuthorizeRequestValidator validator,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var returnUrl = ReadReturnUrl(httpContext.Request.Query["returnUrl"]);

        var user = await AuthorizeEndpoints.ResolveSignedInUser(httpContext, userRepository, cancellationToken);
        if (user is null)
        {
            return Results.Redirect(AuthorizeEndpoints.BuildLocalRedirect(ZuijinEndpointPaths.Login, returnUrl));
        }

        var pending = ReadPendingRequest(returnUrl);

        try
        {
            var (client, _) = await validator.ValidateClientAndRedirectUri(pending, cancellationToken);

            return HtmlPage.Consent(returnUrl, client.ClientName, ParseScopes(pending.Scope));
        }
        catch (Exception exception) when (exception is OAuthException or DomainException)
        {
            return HtmlPage.Error("invalid_request", "The authorization request is no longer valid.",
                StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SubmitConsent(
        HttpContext httpContext,
        AuthorizeRequestValidator validator,
        ConsentService consentService,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var returnUrl = ReadReturnUrl(form["returnUrl"]);

        var user = await AuthorizeEndpoints.ResolveSignedInUser(httpContext, userRepository, cancellationToken);
        if (user is null)
        {
            return Results.Redirect(AuthorizeEndpoints.BuildLocalRedirect(ZuijinEndpointPaths.Login, returnUrl));
        }

        var pending = ReadPendingRequest(returnUrl);

        // Revalidating proves the redirect target still belongs to the client before we send
        // the user agent to it, even on the denial path.
        Domain.Entities.Client client;
        string redirectUri;
        try
        {
            (client, redirectUri) = await validator.ValidateClientAndRedirectUri(pending, cancellationToken);
        }
        catch (Exception exception) when (exception is OAuthException or DomainException)
        {
            return HtmlPage.Error("invalid_request", "The authorization request is no longer valid.",
                StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(form["allow"].ToString(), "true", StringComparison.Ordinal))
        {
            return AuthorizeEndpoints.RedirectWithError(
                redirectUri,
                OAuthError.AccessDenied("The user denied the request."),
                pending.State);
        }

        await consentService.Grant(user.Id, client.Id, ParseScopes(pending.Scope), cancellationToken);

        return Results.Redirect(returnUrl);
    }

    /// <summary>
    /// Rebuilds the authorization request the user was interrupted in, from the return URL.
    /// Keeping it in the URL avoids server-side state between the redirects.
    /// </summary>
    private static AuthorizeRequest ReadPendingRequest(string returnUrl)
    {
        var queryStart = returnUrl.IndexOf('?', StringComparison.Ordinal);
        var query = queryStart < 0
            ? new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()
            : QueryHelpers.ParseQuery(returnUrl[queryStart..]);

        return new AuthorizeRequest
        {
            ClientId = Read(query, "client_id"),
            RedirectUri = Read(query, "redirect_uri"),
            ResponseType = Read(query, "response_type"),
            Scope = Read(query, "scope"),
            State = Read(query, "state")
        };

        static string? Read(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key)
        {
            return query.TryGetValue(key, out var value) ? value.ToString() : null;
        }
    }

    /// <summary>
    /// Only same-site paths are accepted, so a crafted returnUrl cannot bounce the user
    /// to an attacker's site after signing in.
    /// </summary>
    private static string ReadReturnUrl(string? returnUrl)
    {
        var isLocal = !string.IsNullOrEmpty(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal);

        return isLocal ? returnUrl! : "/";
    }

    private static IReadOnlyList<string> ParseScopes(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
