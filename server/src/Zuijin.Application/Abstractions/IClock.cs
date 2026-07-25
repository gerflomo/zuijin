namespace Zuijin.Application.Abstractions;

/// <summary>
/// Abstraction over system time for testability.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
