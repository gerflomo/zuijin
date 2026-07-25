using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing JWT signing keys (RSA key pairs).
/// </summary>
public interface ISigningKeyRepository
{
    Task<SigningKey?> GetActiveKey(CancellationToken cancellationToken = default);
    Task<SigningKey?> GetByKeyId(string keyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SigningKey>> GetAll(CancellationToken cancellationToken = default);
    Task Add(SigningKey key, CancellationToken cancellationToken = default);
    Task Update(SigningKey key, CancellationToken cancellationToken = default);
}
