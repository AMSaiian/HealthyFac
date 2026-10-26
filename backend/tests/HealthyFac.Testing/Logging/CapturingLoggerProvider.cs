using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Testing.Logging;

/// <summary>
/// Records every entry that any component of a host writes, so a test can assert on what the host logged.
/// Register it as an <see cref="ILoggerProvider"/> in the service collection of the host.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public IReadOnlyCollection<CapturedLogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new CapturingCategoryLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingCategoryLogger(string category, ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => entries.Enqueue(
            new CapturedLogEntry
            {
                Level = logLevel,
                Message = formatter(state, exception),
                Category = category,
                Exception = exception,
                Properties = state is IEnumerable<KeyValuePair<string, object?>> values
                    ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
                    : new Dictionary<string, object?>(),
            });
    }
}
