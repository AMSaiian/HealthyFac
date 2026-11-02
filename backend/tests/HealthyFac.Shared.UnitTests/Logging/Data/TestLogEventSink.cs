using Serilog.Core;
using Serilog.Events;

namespace HealthyFac.Shared.UnitTests.Logging.Data;

public sealed class TestLogEventSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent)
    {
        _events.Add(logEvent);
    }
}
