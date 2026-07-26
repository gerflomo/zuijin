using System.Security.Cryptography;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;

namespace Zuijin.Infrastructure.Security;

/// <summary>
/// Protects key material with AES-256-GCM, which provides both confidentiality and
/// integrity: a tampered payload fails to decrypt instead of yielding garbage.
/// Payload layout is [nonce][tag][ciphertext].
/// </summary>
public sealed class AesKeyProtector : IKeyProtector
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _masterKey;

    public AesKeyProtector(ZuijinOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Options validation already guarantees a well-formed 256-bit key at startup.
        _masterKey = Convert.FromBase64String(options.SigningKeyMasterKey!);
    }

    public byte[] Protect(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + plaintext.Length];
        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var tag = payload.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = payload.AsSpan(NonceSizeBytes + TagSizeBytes);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return payload;
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        ArgumentNullException.ThrowIfNull(protectedData);

        if (protectedData.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("The protected payload is too short to be valid.");
        }

        var nonce = protectedData.AsSpan(0, NonceSizeBytes);
        var tag = protectedData.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = protectedData.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
