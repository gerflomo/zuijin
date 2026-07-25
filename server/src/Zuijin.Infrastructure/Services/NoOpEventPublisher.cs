using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Services;

public class NoOpEventPublisher : IEventPublisher
{
    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        return Task.CompletedTask;
    }
}
