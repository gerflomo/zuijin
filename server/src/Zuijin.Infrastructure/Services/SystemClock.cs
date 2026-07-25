using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
