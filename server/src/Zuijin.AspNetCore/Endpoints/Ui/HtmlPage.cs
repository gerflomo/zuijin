using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

namespace Zuijin.AspNetCore.Endpoints.Ui;

/// <summary>
/// Minimal server-rendered pages for the interactive steps of the authorization flow.
/// Every interpolated value is HTML-encoded: these pages echo back client-supplied data.
/// </summary>
public static class HtmlPage
{
    private const string Styles = """
        :root { color-scheme: light dark; }
        * { box-sizing: border-box; }
        body { font-family: system-ui, -apple-system, "Segoe UI", sans-serif; margin: 0;
               min-height: 100vh; display: grid; place-items: center; background: #f4f4f5; }
        @media (prefers-color-scheme: dark) { body { background: #18181b; } .card { background: #27272a; } }
        .card { background: #fff; padding: 2rem; border-radius: .75rem; width: min(24rem, 92vw);
                box-shadow: 0 1px 3px rgb(0 0 0 / .12), 0 8px 24px rgb(0 0 0 / .08); }
        h1 { font-size: 1.25rem; margin: 0 0 1.25rem; }
        label { display: block; font-size: .875rem; margin-bottom: .25rem; }
        input[type=text], input[type=password] { width: 100%; padding: .5rem .625rem; margin-bottom: 1rem;
               border: 1px solid #d4d4d8; border-radius: .375rem; background: transparent; color: inherit; }
        button { padding: .5rem 1rem; border: 0; border-radius: .375rem; cursor: pointer; font-size: .875rem; }
        .primary { background: #4f46e5; color: #fff; width: 100%; }
        .secondary { background: #e4e4e7; color: #18181b; }
        .row { display: flex; gap: .5rem; }
        .row button { flex: 1; }
        .error { background: #fef2f2; border: 1px solid #fecaca; color: #991b1b;
                 padding: .625rem; border-radius: .375rem; margin-bottom: 1rem; font-size: .875rem; }
        ul { margin: 0 0 1.25rem; padding-left: 1.25rem; font-size: .875rem; }
        p { font-size: .875rem; color: #52525b; margin: 0 0 1rem; }
        """;

    public static IResult Login(string returnUrl, string? username, bool hasError)
    {
        var body = new StringBuilder();
        body.Append("<h1>Sign in</h1>");

        if (hasError)
        {
            // Deliberately vague: naming which half was wrong helps enumerate accounts.
            body.Append("""<div class="error">The username or password is incorrect.</div>""");
        }

        body.Append($"""
            <form method="post" action="{Encode(ZuijinEndpointPaths.Login)}">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl)}" />
              <label for="username">Username</label>
              <input id="username" name="username" type="text" autocomplete="username" autofocus
                     value="{Encode(username ?? string.Empty)}" />
              <label for="password">Password</label>
              <input id="password" name="password" type="password" autocomplete="current-password" />
              <button class="primary" type="submit">Sign in</button>
            </form>
            """);

        return Render("Sign in", body.ToString());
    }

    public static IResult Consent(string returnUrl, string clientName, IReadOnlyList<string> scopes)
    {
        var body = new StringBuilder();
        body.Append($"<h1>{Encode(clientName)} wants access</h1>");
        body.Append("<p>It is asking permission to:</p><ul>");

        foreach (var scope in scopes)
        {
            body.Append($"<li>{Encode(scope)}</li>");
        }

        body.Append("</ul>");
        body.Append($"""
            <form method="post" action="{Encode(ZuijinEndpointPaths.Consent)}">
              <input type="hidden" name="returnUrl" value="{Encode(returnUrl)}" />
              <div class="row">
                <button class="secondary" type="submit" name="allow" value="false">Deny</button>
                <button class="primary" type="submit" name="allow" value="true">Allow</button>
              </div>
            </form>
            """);

        return Render("Authorize access", body.ToString());
    }

    /// <summary>
    /// Used when a request cannot be safely redirected back to the client, which is the case
    /// whenever the client or its redirect URI could not be verified.
    /// </summary>
    public static IResult Error(string error, string? description, int statusCode)
    {
        var body = $"""
            <h1>{Encode(error)}</h1>
            <p>{Encode(description ?? "The request could not be processed.")}</p>
            """;

        return Render("Request failed", body, statusCode);
    }

    private static IResult Render(string title, string body, int statusCode = StatusCodes.Status200OK)
    {
        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{Encode(title)}</title>
              <style>{Styles}</style>
            </head>
            <body><main class="card">{body}</main></body>
            </html>
            """;

        return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8, statusCode);
    }

    private static string Encode(string value)
    {
        return HtmlEncoder.Default.Encode(value);
    }
}
