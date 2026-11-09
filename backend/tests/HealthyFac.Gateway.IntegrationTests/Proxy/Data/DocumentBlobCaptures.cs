using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;
using Serilog.Core;
using Serilog.Events;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

/// <summary>
/// Records every finished span, so a test can look for a secret in the tags.
/// </summary>
public sealed class SpanTagCapture : BaseProcessor<Activity>
{
    private readonly ConcurrentQueue<string> _tagValues = new();

    public IReadOnlyCollection<string> TagValues => _tagValues;

    public override void OnEnd(Activity data)
    {
        foreach (var tag in data.TagObjects)
        {
            _tagValues.Enqueue($"{tag.Key}={tag.Value}");
        }
    }
}

/// <summary>
/// Records every Serilog event as text: the rendered message and each property.
/// </summary>
public sealed class LogTextCapture : ILogEventSink
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyCollection<string> Lines => _lines;

    public void Emit(LogEvent logEvent)
    {
        var properties = string.Join(separator: ' ', logEvent.Properties.Select(pair => $"{pair.Key}={pair.Value}"));
        _lines.Enqueue($"{logEvent.RenderMessage()} {properties}");
    }
}
