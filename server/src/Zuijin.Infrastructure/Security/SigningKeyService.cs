using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Security;

/// <summary>
/// Manages the RSA key pairs used to sign JWTs. Private keys are stored encrypted
/// and the decrypted material is cached briefly to keep token issuance off the database.
/// </summary>
public sealed class SigningKeyService : ISigningKeyService, IDisposable
{
    private const int RsaKeySizeBits = 2048;
    private const string DefaultAlgorithm = "RS256";

    /// <summary>
    /// A retired key stays published in JWKS for twice the longest JWT lifetime, so
    /// tokens signed just before a rotation can still be validated afterwards.
    /// </summary>
    private const int KeyOverlapFactor = 2;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKeyProtector _keyProtector;
    private readonly IClock _clock;
    private readonly ZuijinOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CachedSigningKey? _cache;

    public SigningKeyService(
        IServiceScopeFactory scopeFactory,
        IKeyProtector keyProtector,
        IClock clock,
        ZuijinOptions options)
    {
        _scopeFactory = scopeFactory;
        _keyProtector = keyProtector;
        _clock = clock;
        _options = options;
    }

    /// <summary>
    /// Returns the active signing key. The caller owns the returned
    /// <see cref="SigningKeyInfo.Key"/> instance and must dispose it.
    /// </summary>
    public async Task<SigningKeyInfo> GetActiveSigningKey(CancellationToken cancellationToken = default)
    {
        var cached = _cache;
        if (cached is not null && cached.ExpiresAt > _clock.UtcNow)
        {
            return Materialize(cached);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            cached = _cache;
            if (cached is not null && cached.ExpiresAt > _clock.UtcNow)
            {
                return Materialize(cached);
            }

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();

            var key = await repository.GetActiveKey(cancellationToken)
                ?? throw new InvalidOperationException(
                    "No active signing key was found. The signing key maintenance service creates one at startup.");

            cached = new CachedSigningKey(
                key.KeyId,
                key.Algorithm,
                _keyProtector.Unprotect(key.KeyData),
                _clock.UtcNow.Add(CacheLifetime));

            _cache = cached;
            return Materialize(cached);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<JsonWebKeyInfo>> GetPublicKeys(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();

        var keys = await repository.GetAll(cancellationToken);
        var now = _clock.UtcNow;
        var publicKeys = new List<JsonWebKeyInfo>();

        foreach (var key in keys)
        {
            // Retired keys stay published until they expire so previously issued tokens remain verifiable.
            var isPublishable = key.IsActive || (key.ExpiresAt is not null && key.ExpiresAt > now);
            if (!isPublishable)
            {
                continue;
            }

            publicKeys.Add(ToJsonWebKey(key));
        }

        return publicKeys;
    }

    public async Task RotateKeys(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var now = _clock.UtcNow;

            var current = await repository.GetActiveKey(cancellationToken);
            if (current is not null)
            {
                current.IsActive = false;
                current.ExpiresAt = now.Add(GetOverlapWindow());
                await repository.Update(current, cancellationToken);
            }

            await repository.Add(CreateKey(now), cancellationToken);
            await unitOfWork.SaveChanges(cancellationToken);

            _cache = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private SigningKey CreateKey(DateTimeOffset now)
    {
        using var rsa = RSA.Create(RsaKeySizeBits);
        var privateKey = rsa.ExportRSAPrivateKey();

        try
        {
            return new SigningKey
            {
                KeyId = Guid.CreateVersion7().ToString("N"),
                Algorithm = DefaultAlgorithm,
                KeyData = _keyProtector.Protect(privateKey),
                IsActive = true,
                ActivatedAt = now
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private JsonWebKeyInfo ToJsonWebKey(SigningKey key)
    {
        var privateKey = _keyProtector.Unprotect(key.KeyData);

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            var parameters = rsa.ExportParameters(includePrivateParameters: false);

            return new JsonWebKeyInfo
            {
                KeyId = key.KeyId,
                KeyType = JsonWebAlgorithmsKeyTypes.RSA,
                Algorithm = key.Algorithm,
                Use = "sig",
                Modulus = Base64UrlEncoder.Encode(parameters.Modulus),
                Exponent = Base64UrlEncoder.Encode(parameters.Exponent)
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private TimeSpan GetOverlapWindow()
    {
        var longestJwtLifetime = Math.Max(
            _options.DefaultAccessTokenLifetime!.Value,
            _options.DefaultIdTokenLifetime!.Value);

        return TimeSpan.FromSeconds(longestJwtLifetime * KeyOverlapFactor);
    }

    private static SigningKeyInfo Materialize(CachedSigningKey cached)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(cached.PrivateKey, out _);

        return new SigningKeyInfo
        {
            KeyId = cached.KeyId,
            Algorithm = cached.Algorithm,
            Key = rsa
        };
    }

    private sealed record CachedSigningKey(
        string KeyId,
        string Algorithm,
        byte[] PrivateKey,
        DateTimeOffset ExpiresAt);
}
