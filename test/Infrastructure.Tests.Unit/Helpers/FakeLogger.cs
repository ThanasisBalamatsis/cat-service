using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Unit.Helpers;

public class FakeLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private class NullScope : IDisposable { public static NullScope Instance { get; } = new(); public void Dispose() { } }
}
