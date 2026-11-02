using Microsoft.Extensions.Logging;

namespace HealthyFac.Testing.Logging;

/// <summary>
/// One log entry that a <see cref="CapturingLogger{T}"/> recorded.
/// </summary>
public sealed class CapturedLogEntry
{
    public required LogLevel Level { get; init; }

    public required string Message { get; init; }

    public required string Category { get; init; }

    public Exception? Exception { get; init; }

    /// <summary>The structured values of the message template, keyed by placeholder name.</summary>
    public required IReadOnlyDictionary<string, object?> Properties { get; init; }
}
