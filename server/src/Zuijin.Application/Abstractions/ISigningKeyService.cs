using System.Security.Cryptography;

namespace Zuijin.Application.Abstractions;

/// <summary>
/// Manages RSA signing keys for JWT generation and JWKS endpoint.
/// </summary>
public interface ISigningKeyService
{
    Task<SigningKeyInfo> GetActiveSigningKey(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JsonWebKeyInfo>> GetPublicKeys(CancellationToken cancellationToken = default);
    Task RotateKeys(CancellationToken cancellationToken = default);
}

public class SigningKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public RSA Key { get; set; } = null!;
}

public class JsonWebKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Use { get; set; } = "sig";
    public string Modulus { get; set; } = string.Empty;
    public string Exponent { get; set; } = string.Empty;
}
