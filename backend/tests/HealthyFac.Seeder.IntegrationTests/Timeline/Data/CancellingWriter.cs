namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

/// <summary>A log writer that stops the job when a log line holds a text, like a process stop at that event.</summary>
public sealed class CancellingWriter(string text, CancellationTokenSource stop) : StringWriter
{
    private readonly string _text = text;
    private readonly CancellationTokenSource _stop = stop;

    public override void WriteLine(string? value)
    {
        base.WriteLine(value);
        if (value is not null && value.Contains(_text, StringComparison.Ordinal))
        {
            _stop.Cancel();
        }
    }
}
