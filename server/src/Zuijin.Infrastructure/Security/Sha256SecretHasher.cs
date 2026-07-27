using System.Security.Cryptography;
using System.Text;
using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Security;

/// <summary>
/// SHA-256 digest of high-entropy secrets, stored base64.
/// A slow KDF is deliberately avoided here: client credentials are exchanged on a
/// machine-to-machine hot path, so an expensive hash would become a denial-of-service lever,
/// and the secrets carry enough entropy that brute force is not the threat.
/// </summary>
public class Sha256SecretHasher : ISecretHasher
{
    private const int Sha256SizeBytes = 32;

    public string Hash(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    public bool Verify(string secret, string hash)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        Span<byte> stored = stackalloc byte[Sha256SizeBytes];
        if (!Convert.TryFromBase64String(hash, stored, out var bytesWritten) || bytesWritten != Sha256SizeBytes)
        {
            return false;
        }

        Span<byte> computed = stackalloc byte[Sha256SizeBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(secret), computed);

        return CryptographicOperations.FixedTimeEquals(computed, stored);
    }
}
