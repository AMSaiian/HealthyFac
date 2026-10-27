using Serilog.Core;
using Serilog.Events;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;

public sealed class CapturingLogEventSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events => _events;

    public void Emit(LogEvent logEvent) => _events.Add(logEvent);
}
