using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Zuijin.Application.Features.Authorization;
using Zuijin.AspNetCore.Authentication;
using Zuijin.AspNetCore.Endpoints.Ui;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;

namespace Zuijin.AspNetCore.Endpoints.Authorize;

/// <summary>
/// The OAuth 2.0 authorization endpoint: the browser-facing half of the authorization code flow.
/// </summary>
public static class AuthorizeEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ZuijinEndpointPaths.Authorize, Authorize).WithName("Authorize");

        return endpoints;
    }

    private static async Task<IResult> Authorize(
        HttpContext httpContext,
        AuthorizeRequestValidator validator,
        ConsentService consentService,
        AuthorizationCodeIssuer codeIssuer,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var request = ReadRequest(httpContext.Request.Query);

        // Stage one. Until the client and its redirect URI are proven, an error may only be
        // shown to the user: redirecting it would make this endpoint an open redirector.
        Domain.Entities.Client client;
        string redirectUri;
        try
        {
            (client, redirectUri) = await validator.ValidateClientAndRedirectUri(request, cancellationToken);
        }
        catch (OAuthException exception)
        {
            return HtmlPage.Error(exception.Error.Error, exception.Error.ErrorDescription, StatusCodes.Status400BadRequest);
        }
        catch (DomainException exception)
        {
            var error = OAuthErrorTranslator.Translate(exception);
            return HtmlPage.Error(error.Error, error.ErrorDescription, StatusCodes.Status400BadRequest);
        }

        // Stage two. The redirect target is trusted now, so errors travel back to the client.
        ValidatedAuthorizeRequest validated;
        try
        {
            validated = validator.Validate(request, client, redirectUri);
        }
        catch (OAuthException exception)
        {
            return RedirectWithError(redirectUri, exception.Error, request.State);
        }
        catch (DomainException exception)
        {
            return RedirectWithError(redirectUri, OAuthErrorTranslator.Translate(exception), request.State);
        }

        var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;

        var user = await ResolveSignedInUser(httpContext, userRepository, cancellationToken);
        if (user is null)
        {
            return Results.Redirect(BuildLocalRedirect(ZuijinEndpointPaths.Login, returnUrl));
        }

        if (client.RequireConsent
            && !await consentService.HasConsentFor(user.Id, client.Id, validated.Scopes, cancellationToken))
        {
            return Results.Redirect(BuildLocalRedirect(ZuijinEndpointPaths.Consent, returnUrl));
        }

        var code = await codeIssuer.Issue(validated, user.Id, cancellationToken);

        var parameters = new Dictionary<string, string?> { ["code"] = code };
        AddStateIfPresent(parameters, request.State);

        return Results.Redirect(QueryHelpers.AddQueryString(redirectUri, parameters));
    }

    internal static AuthorizeRequest ReadRequest(IQueryCollection query)
    {
        return new AuthorizeRequest
        {
            ClientId = query["client_id"],
            RedirectUri = query["redirect_uri"],
            ResponseType = query["response_type"],
            Scope = query["scope"],
            State = query["state"],
            CodeChallenge = query["code_challenge"],
            CodeChallengeMethod = query["code_challenge_method"],
            Nonce = query["nonce"]
        };
    }

    internal static async Task<Domain.Entities.User?> ResolveSignedInUser(
        HttpContext httpContext,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var authentication = await httpContext.AuthenticateAsync(ZuijinAuthenticationDefaults.SessionScheme);

        var subjectId = authentication.Succeeded
            ? authentication.Principal?.FindFirstValue(ZuijinAuthenticationDefaults.SubjectClaimType)
            : null;

        if (string.IsNullOrEmpty(subjectId))
        {
            return null;
        }

        var user = await userRepository.GetBySubjectId(subjectId, cancellationToken);

        // The session outlived the account: drop the cookie so the user can sign in again.
        if (user is null || !user.IsActive)
        {
            await httpContext.SignOutAsync(ZuijinAuthenticationDefaults.SessionScheme);
            return null;
        }

        return user;
    }

    internal static IResult RedirectWithError(string redirectUri, OAuthError error, string? state)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["error"] = error.Error,
            ["error_description"] = error.ErrorDescription
        };

        AddStateIfPresent(parameters, state);

        return Results.Redirect(QueryHelpers.AddQueryString(redirectUri, parameters));
    }

    internal static string BuildLocalRedirect(string path, string returnUrl)
    {
        return QueryHelpers.AddQueryString(path, "returnUrl", returnUrl);
    }

    private static void AddStateIfPresent(Dictionary<string, string?> parameters, string? state)
    {
        // state is opaque to the server and must be echoed back untouched when present.
        if (!string.IsNullOrEmpty(state))
        {
            parameters["state"] = state;
        }
    }
}
