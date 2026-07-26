using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Zuijin.Application.Configuration;
using Zuijin.Infrastructure.Security;

namespace Zuijin.Infrastructure.Tests.Security;

public class AesKeyProtectorTests
{
    private static readonly byte[] Secret = Encoding.UTF8.GetBytes("an RSA private key blob");

    private static AesKeyProtector CreateProtector(byte[]? masterKey = null)
    {
        var key = masterKey ?? RandomNumberGenerator.GetBytes(32);
        return new AesKeyProtector(new ZuijinOptions { SigningKeyMasterKey = Convert.ToBase64String(key) });
    }

    [Fact]
    public void Unprotect_ProtectedPayload_ReturnsOriginalPlaintext()
    {
        // Arrange
        var protector = CreateProtector();

        // Act
        var roundTripped = protector.Unprotect(protector.Protect(Secret));

        // Assert
        roundTripped.ShouldBe(Secret);
    }

    [Fact]
    public void Protect_SamePlaintextTwice_ProducesDifferentPayloads()
    {
        // Arrange
        var protector = CreateProtector();

        // Act
        var first = protector.Protect(Secret);
        var second = protector.Protect(Secret);

        // Assert: a random nonce per call must prevent identical ciphertexts.
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Protect_Payload_DoesNotContainPlaintext()
    {
        // Arrange
        var protector = CreateProtector();

        // Act
        var payload = protector.Protect(Secret);

        // Assert
        Convert.ToBase64String(payload).ShouldNotContain(Convert.ToBase64String(Secret));
    }

    [Fact]
    public void Unprotect_TamperedCiphertext_Throws()
    {
        // Arrange
        var protector = CreateProtector();
        var payload = protector.Protect(Secret);
        payload[^1] ^= 0xFF;

        // Act & Assert: GCM authentication must reject modified payloads.
        Should.Throw<CryptographicException>(() => protector.Unprotect(payload));
    }

    [Fact]
    public void Unprotect_TamperedNonce_Throws()
    {
        // Arrange
        var protector = CreateProtector();
        var payload = protector.Protect(Secret);
        payload[0] ^= 0xFF;

        // Act & Assert
        Should.Throw<CryptographicException>(() => protector.Unprotect(payload));
    }

    [Fact]
    public void Unprotect_DifferentMasterKey_Throws()
    {
        // Arrange
        var payload = CreateProtector().Protect(Secret);
        var otherProtector = CreateProtector();

        // Act & Assert
        Should.Throw<CryptographicException>(() => otherProtector.Unprotect(payload));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(27)]
    public void Unprotect_PayloadShorterThanHeader_Throws(int length)
    {
        // Arrange
        var protector = CreateProtector();

        // Act & Assert
        Should.Throw<CryptographicException>(() => protector.Unprotect(new byte[length]));
    }
}
