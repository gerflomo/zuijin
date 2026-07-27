using Microsoft.AspNetCore.Http;
using Zuijin.AspNetCore.Endpoints.Token;
using Zuijin.Domain.Errors;

namespace Zuijin.AspNetCore.Endpoints;

/// <summary>
/// Turns protocol failures into RFC 6749 error responses so every OAuth endpoint
/// answers with the same shape instead of leaking an unhandled exception.
/// </summary>
public sealed class OAuthErrorEndpointFilter : IEndpointFilter
{
    private const string BasicChallenge = "Basic realm=\"Zuijin\"";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (OAuthException exception)
        {
            return CreateErrorResult(context.HttpContext, exception.Error);
        }
        catch (DomainException exception)
        {
            return CreateErrorResult(context.HttpContext, OAuthErrorTranslator.Translate(exception));
        }
    }

    private static IResult CreateErrorResult(HttpContext httpContext, OAuthError error)
    {
        var statusCode = error.Error switch
        {
            OAuthErrorCodes.InvalidClient => StatusCodes.Status401Unauthorized,
            OAuthErrorCodes.ServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = BasicChallenge;
        }

        return Results.Json(
            new OAuthErrorDocument
            {
                Error = error.Error,
                ErrorDescription = error.ErrorDescription
            },
            statusCode: statusCode);
    }
}
