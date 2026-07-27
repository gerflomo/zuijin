using Shouldly;
using Zuijin.Infrastructure.Security;

namespace Zuijin.Infrastructure.Tests.Security;

public class Sha256SecretHasherTests
{
    private const string Secret = "a-generated-client-secret";

    private readonly Sha256SecretHasher _hasher = new();

    [Fact]
    public void Hash_SameSecretTwice_ProducesTheSameHash()
    {
        // Act & Assert: the hash must be deterministic so tokens can be looked up by it.
        _hasher.Hash(Secret).ShouldBe(_hasher.Hash(Secret));
    }

    [Fact]
    public void Hash_DifferentSecrets_ProduceDifferentHashes()
    {
        // Act & Assert
        _hasher.Hash(Secret).ShouldNotBe(_hasher.Hash("another-secret"));
    }

    [Fact]
    public void Hash_AnySecret_DoesNotContainThePlaintext()
    {
        // Act & Assert
        _hasher.Hash(Secret).ShouldNotContain(Secret);
    }

    [Fact]
    public void Verify_CorrectSecret_ReturnsTrue()
    {
        // Arrange
        var hash = _hasher.Hash(Secret);

        // Act & Assert
        _hasher.Verify(Secret, hash).ShouldBeTrue();
    }

    [Theory]
    [InlineData("wrong-secret")]
    [InlineData("a-generated-client-secre")]
    [InlineData("A-Generated-Client-Secret")]
    public void Verify_WrongSecret_ReturnsFalse(string candidate)
    {
        // Arrange
        var hash = _hasher.Hash(Secret);

        // Act & Assert
        _hasher.Verify(candidate, hash).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64!!")]
    [InlineData("dG9vLXNob3J0")]
    public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing(string hash)
    {
        // Act & Assert
        _hasher.Verify(Secret, hash).ShouldBeFalse();
    }

    [Fact]
    public void Verify_EmptySecret_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.Hash(Secret);

        // Act & Assert
        _hasher.Verify(string.Empty, hash).ShouldBeFalse();
    }
}
