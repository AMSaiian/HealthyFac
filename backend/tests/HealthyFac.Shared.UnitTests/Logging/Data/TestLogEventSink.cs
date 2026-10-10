using Serilog.Core;
using Serilog.Events;

namespace HealthyFac.Shared.UnitTests.Logging.Data;

public sealed class TestLogEventSink : ILogEventSink
{
    private readonly List<LogEvent> events = [];

    public IReadOnlyList<LogEvent> Events => events;

    public void Emit(LogEvent logEvent)
    {
        events.Add(logEvent);
    }
}
