namespace Zuijin.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords using a secure algorithm (Argon2id).
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
