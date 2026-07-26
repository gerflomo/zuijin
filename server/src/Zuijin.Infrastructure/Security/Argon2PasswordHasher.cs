using Isopoh.Cryptography.Argon2;
using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Security;

/// <summary>
/// Argon2id password hasher with explicit work-factor parameters
/// (OWASP password storage recommendation: 19 MiB memory, 2 iterations, 1 lane).
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int TimeCost = 2;
    private const int MemoryCostKibibytes = 19456;
    private const int Parallelism = 1;
    private const int HashLengthBytes = 32;

    public string HashPassword(string password)
    {
        return Argon2.Hash(
            password,
            timeCost: TimeCost,
            memoryCost: MemoryCostKibibytes,
            parallelism: Parallelism,
            type: Argon2Type.HybridAddressing,
            hashLength: HashLengthBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return Argon2.Verify(hash, password);
    }

    public bool NeedsRehash(string hash)
    {
        var config = new Argon2Config();
        if (!config.DecodeString(hash, out var decodedHash))
        {
            return true;
        }

        decodedHash?.Dispose();

        return config.Type != Argon2Type.HybridAddressing
            || config.TimeCost < TimeCost
            || config.MemoryCost < MemoryCostKibibytes
            || config.Lanes != Parallelism;
    }
}
