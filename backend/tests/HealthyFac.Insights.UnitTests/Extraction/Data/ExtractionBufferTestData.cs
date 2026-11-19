using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class ExtractionBufferTestData
{
    /// <summary>The case name, whether the document is a machine file, and the resolution that must fail.</summary>
    public static TheoryData<string, bool, Func<ExtractionBuffer, Guid, ResolvedSignal>> UnknownKeys => new()
    {
        { "machine file, unknown metric", true, (buffer, id) => buffer.ResolveMetric(id, key: "unknown.key") },
        { "machine file, unknown status", true, (buffer, id) => buffer.ResolveStatus(id, code: "E99") },
        {
            "company file, key of another machine",
            false,
            (buffer, id) => buffer.ResolveMetric(id, key: "motor.temperature")
        },
        { "company file, code of another machine", false, (buffer, id) => buffer.ResolveStatus(id, code: "E12") },
    };

    public static ProposedMetric Metric(string key) => new() { DraftId = Guid.CreateVersion7(), Key = key };
}
