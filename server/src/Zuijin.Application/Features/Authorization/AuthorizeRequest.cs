using Zuijin.Domain.Entities;

namespace Zuijin.Application.Features.Authorization;

/// <summary>
/// The raw query parameters of an authorization request (RFC 6749 section 4.1.1).
/// </summary>
public sealed class AuthorizeRequest
{
    public required string? ClientId { get; init; }
    public required string? RedirectUri { get; init; }
    public required string? ResponseType { get; init; }
    public string? Scope { get; init; }
    public string? State { get; init; }
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Nonce { get; init; }
}

/// <summary>
/// An authorization request whose client and redirect URI have been verified, so any
/// remaining error is safe to report back to the client by redirection.
/// </summary>
public sealed class ValidatedAuthorizeRequest
{
    public required Client Client { get; init; }
    public required string RedirectUri { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
    public string? State { get; init; }
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Nonce { get; init; }
}
