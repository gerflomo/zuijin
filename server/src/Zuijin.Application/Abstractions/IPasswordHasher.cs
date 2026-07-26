namespace Zuijin.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords using a secure algorithm (Argon2id).
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Returns true when the stored hash was produced with parameters weaker than the
    /// current configuration and should be re-hashed on the next successful login.
    /// </summary>
    bool NeedsRehash(string hash);
}
