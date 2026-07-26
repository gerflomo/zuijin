using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Services;

/// <summary>
/// Creates the initial signing key before the host starts serving traffic and
/// rotates it once it reaches the configured rotation interval.
/// </summary>
public sealed class SigningKeyMaintenanceService : IHostedService, IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISigningKeyService _signingKeyService;
    private readonly IClock _clock;
    private readonly ZuijinOptions _options;
    private readonly ILogger<SigningKeyMaintenanceService> _logger;

    private CancellationTokenSource? _stoppingTokenSource;
    private Task? _maintenanceLoop;

    public SigningKeyMaintenanceService(
        IServiceScopeFactory scopeFactory,
        ISigningKeyService signingKeyService,
        IClock clock,
        ZuijinOptions options,
        ILogger<SigningKeyMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _signingKeyService = signingKeyService;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Awaited so the host cannot accept requests before a signing key exists.
        await RotateIfNeeded(cancellationToken);

        _stoppingTokenSource = new CancellationTokenSource();
        _maintenanceLoop = RunMaintenanceLoop(_stoppingTokenSource.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stoppingTokenSource is null || _maintenanceLoop is null)
        {
            return;
        }

        await _stoppingTokenSource.CancelAsync();

        try
        {
            await _maintenanceLoop.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Signing key maintenance loop did not stop before the shutdown timeout.");
        }
    }

    public void Dispose()
    {
        _stoppingTokenSource?.Dispose();
    }

    private async Task RunMaintenanceLoop(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        while (await SafeWaitForNextTick(timer, cancellationToken))
        {
            try
            {
                await RotateIfNeeded(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // A transient failure must not kill the loop; the next tick retries.
                _logger.LogError(ex, "Signing key rotation check failed.");
            }
        }
    }

    private static async Task<bool> SafeWaitForNextTick(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RotateIfNeeded(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();

        var activeKey = await repository.GetActiveKey(cancellationToken);

        if (activeKey is null)
        {
            _logger.LogInformation("No active signing key found; creating the initial key.");
            await _signingKeyService.RotateKeys(cancellationToken);
            return;
        }

        var rotationDue = activeKey.ActivatedAt.AddDays(_options.KeyRotationIntervalDays!.Value);
        if (rotationDue <= _clock.UtcNow)
        {
            _logger.LogInformation("Signing key {KeyId} reached its rotation interval; rotating.", activeKey.KeyId);
            await _signingKeyService.RotateKeys(cancellationToken);
        }
    }
}
