using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public sealed record TestRequest : IRequest<int>;

public sealed class ThrowingHandler : IRequestHandler<TestRequest, int>
{
    private readonly Exception _exception;

    public ThrowingHandler(Exception exception)
    {
        _exception = exception;
    }

    public Task<int> HandleAsync(TestRequest request, CancellationToken ct = default)
        => throw _exception;
}

public sealed class ErrorLevelRejectionException : Exception, IRejection
{
    public LogLevel? Level => LogLevel.Error;
}

public sealed record LogEntry
{
    public required LogLevel Level { get; init; }

    public required string Message { get; init; }

    public Exception? Exception { get; init; }

    public required IReadOnlyDictionary<string, object?> Properties { get; init; }
}

public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
        => Entries.Add(
            new LogEntry
            {
                Level = logLevel,
                Message = formatter(state, exception),
                Exception = exception,
                Properties = ReadProperties(state),
            });

    private static Dictionary<string, object?> ReadProperties<TState>(TState state)
        => state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
            : [];
}
