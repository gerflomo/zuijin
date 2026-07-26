namespace Zuijin.Application.Abstractions;

/// <summary>
/// Encrypts and decrypts sensitive material before it is persisted.
/// Used to protect RSA private keys stored in the SigningKeys table.
/// </summary>
public interface IKeyProtector
{
    byte[] Protect(byte[] plaintext);

    /// <summary>
    /// Reverses <see cref="Protect"/>. Throws when the payload was tampered with
    /// or was produced with a different master key.
    /// </summary>
    byte[] Unprotect(byte[] protectedData);
}
