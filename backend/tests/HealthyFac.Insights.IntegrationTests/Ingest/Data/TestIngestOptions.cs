using HealthyFac.Insights.Application.Ingest.Processing;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// The ingest options of the tests. A test replaces <see cref="Value"/>, for example with a run limit of 1 second. The
/// scoped use case reads the value when its scope starts, so the change applies to the next run.
/// </summary>
public sealed class TestIngestOptions : IOptions<IngestProcessingOptions>
{
    private const int TestRetryDelayMilliseconds = 1;

    public IngestProcessingOptions Value { get; set; } = CreateDefault();

    public void Reset() => Value = CreateDefault();

    private static IngestProcessingOptions CreateDefault() =>
        new() { RetryDelayMilliseconds = TestRetryDelayMilliseconds };
}
