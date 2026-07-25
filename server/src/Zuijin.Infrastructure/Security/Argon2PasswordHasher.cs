using Isopoh.Cryptography.Argon2;
using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Security;

public class Argon2PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return Argon2.Hash(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return Argon2.Verify(hash, password);
    }
}
