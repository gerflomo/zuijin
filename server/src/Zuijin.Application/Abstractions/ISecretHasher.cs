namespace Zuijin.Application.Abstractions;

/// <summary>
/// Hashes high-entropy machine-generated secrets such as client secrets and tokens.
/// Distinct from <see cref="IPasswordHasher"/>: user passwords are low entropy and need a
/// slow KDF, whereas these values are random enough that a fast digest is sufficient.
/// </summary>
public interface ISecretHasher
{
    string Hash(string secret);

    /// <summary>Compares in constant time so the result cannot be probed by timing.</summary>
    bool Verify(string secret, string hash);
}
