using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Testing.Logging;

/// <summary>
/// Records every entry that a component writes, so a test can assert on the level, the message, and the
/// structured values.
/// </summary>
/// <typeparam name="T">The type that owns the logger category.</typeparam>
public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public IReadOnlyCollection<CapturedLogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) => _entries.Enqueue(
        new CapturedLogEntry
        {
            Level = logLevel,
            Message = formatter(state, exception),
            Category = typeof(T).FullName ?? typeof(T).Name,
            Exception = exception,
            Properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>(),
        });
}
