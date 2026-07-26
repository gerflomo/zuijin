using System.Text.Json.Serialization;

namespace Zuijin.AspNetCore.Endpoints.Discovery;

/// <summary>
/// JWK Set (RFC 7517) exposing the public halves of the active and recently retired signing keys.
/// </summary>
public sealed record JsonWebKeySetDocument
{
    [JsonPropertyName("keys")]
    public required IReadOnlyList<JsonWebKeyDocument> Keys { get; init; }
}

public sealed record JsonWebKeyDocument
{
    [JsonPropertyName("kty")]
    public required string KeyType { get; init; }

    [JsonPropertyName("use")]
    public required string Use { get; init; }

    [JsonPropertyName("kid")]
    public required string KeyId { get; init; }

    [JsonPropertyName("alg")]
    public required string Algorithm { get; init; }

    [JsonPropertyName("n")]
    public required string Modulus { get; init; }

    [JsonPropertyName("e")]
    public required string Exponent { get; init; }
}
