using Zuijin.Application.Abstractions;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Features.Authorization;

/// <summary>
/// Remembers which scopes a user has already granted to a client.
/// </summary>
public sealed class ConsentService
{
    private readonly IConsentRepository _consentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConsentService(IConsentRepository consentRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _consentRepository = consentRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// True when a stored consent covers every requested scope. Asking for anything new
    /// sends the user back to the consent screen.
    /// </summary>
    public async Task<bool> HasConsentFor(
        Guid userId,
        Guid clientId,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndClient(userId, clientId, cancellationToken);

        if (consent is null || (consent.ExpiresAt is not null && consent.ExpiresAt <= _clock.UtcNow))
        {
            return false;
        }

        var consented = consent.Scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var scope in scopes)
        {
            if (!consented.Contains(scope))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Stores the granted scopes, merging them with anything previously consented.
    /// </summary>
    public async Task Grant(
        Guid userId,
        Guid clientId,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndClient(userId, clientId, cancellationToken);
        var granted = new SortedSet<string>(scopes, StringComparer.Ordinal);

        if (consent is null)
        {
            await _consentRepository.Add(new Consent
            {
                UserId = userId,
                ClientId = clientId,
                Scopes = string.Join(' ', granted)
            }, cancellationToken);
        }
        else
        {
            foreach (var scope in consent.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                granted.Add(scope);
            }

            consent.Scopes = string.Join(' ', granted);
            await _consentRepository.Update(consent, cancellationToken);
        }

        await _unitOfWork.SaveChanges(cancellationToken);
    }
}
