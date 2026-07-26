using Isopoh.Cryptography.Argon2;
using Shouldly;
using Zuijin.Infrastructure.Security;

namespace Zuijin.Infrastructure.Tests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ValidPassword_ProducesArgon2idEncodedHash()
    {
        // Act
        var hash = _hasher.HashPassword("correct horse battery staple");

        // Assert
        hash.ShouldStartWith("$argon2id$");
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Arrange
        const string password = "correct horse battery staple";

        // Act
        var first = _hasher.HashPassword(password);
        var second = _hasher.HashPassword(password);

        // Assert: a random salt must make every hash unique.
        first.ShouldNotBe(second);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string password = "correct horse battery staple";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("correct horse battery staple");

        // Act
        var result = _hasher.VerifyPassword("wrong password", hash);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void NeedsRehash_FreshHash_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("correct horse battery staple");

        // Act
        var result = _hasher.NeedsRehash(hash);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void NeedsRehash_MalformedHash_ReturnsTrue()
    {
        // Act
        var result = _hasher.NeedsRehash("not-an-argon2-hash");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void NeedsRehash_HashWithWeakerParameters_ReturnsTrue()
    {
        // Arrange: a legacy hash produced with a lower memory cost than the current policy.
        var weakHash = Argon2.Hash(
            "correct horse battery staple",
            timeCost: 1,
            memoryCost: 1024,
            parallelism: 1,
            type: Argon2Type.HybridAddressing,
            hashLength: 32);

        // Act
        var result = _hasher.NeedsRehash(weakHash);

        // Assert
        result.ShouldBeTrue();
    }
}
