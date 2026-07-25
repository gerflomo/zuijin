namespace Zuijin.Application.Abstractions;

/// <summary>
/// Publishes domain events for extensibility and decoupling.
/// </summary>
public interface IEventPublisher
{
    Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
