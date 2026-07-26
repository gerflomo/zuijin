using System.Text.Json.Serialization;

namespace Zuijin.AspNetCore.Endpoints.Discovery;

/// <summary>
/// OpenID Provider metadata as defined by OpenID Connect Discovery 1.0.
/// Property names are fixed by the specification, hence the explicit JSON names.
/// </summary>
public sealed record DiscoveryDocument
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; init; }

    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    [JsonPropertyName("userinfo_endpoint")]
    public required string UserInfoEndpoint { get; init; }

    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; init; }

    [JsonPropertyName("revocation_endpoint")]
    public required string RevocationEndpoint { get; init; }

    [JsonPropertyName("introspection_endpoint")]
    public required string IntrospectionEndpoint { get; init; }

    [JsonPropertyName("device_authorization_endpoint")]
    public required string DeviceAuthorizationEndpoint { get; init; }

    [JsonPropertyName("scopes_supported")]
    public required IReadOnlyList<string> ScopesSupported { get; init; }

    [JsonPropertyName("response_types_supported")]
    public required IReadOnlyList<string> ResponseTypesSupported { get; init; }

    [JsonPropertyName("grant_types_supported")]
    public required IReadOnlyList<string> GrantTypesSupported { get; init; }

    [JsonPropertyName("subject_types_supported")]
    public required IReadOnlyList<string> SubjectTypesSupported { get; init; }

    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public required IReadOnlyList<string> IdTokenSigningAlgValuesSupported { get; init; }

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public required IReadOnlyList<string> TokenEndpointAuthMethodsSupported { get; init; }

    [JsonPropertyName("code_challenge_methods_supported")]
    public required IReadOnlyList<string> CodeChallengeMethodsSupported { get; init; }
}
