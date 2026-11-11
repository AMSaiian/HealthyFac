using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.IntegrationTests.Database.Data;

/// <summary>
/// Records the log entries of the test service provider, so a test counts the warnings of a component.
/// </summary>
public sealed class LogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _entries = new();

    public IReadOnlyList<CapturedLog> Entries => [.. _entries];

    public void Clear() => _entries.Clear();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> entries) : ILogger
    {
        private readonly string _category = category;
        private readonly ConcurrentQueue<CapturedLog> _entries = entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => _entries.Enqueue(new CapturedLog(_category, logLevel, formatter(state, exception)));
    }
}

/// <summary>
/// One recorded log entry.
/// </summary>
/// <param name="Category">The category of the logger.</param>
/// <param name="Level">The level of the entry.</param>
/// <param name="Message">The formatted message.</param>
public sealed record CapturedLog(string Category, LogLevel Level, string Message);
