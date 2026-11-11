using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.ApiTests.Ingest.Data;

public static class CancelIngestAttemptTestData
{
    public static string CancelPath(string encryptedAttemptId) => $"/v1/ingest-attempts/{encryptedAttemptId}/cancel";

    public static TheoryData<IngestState> CancellableStates => new()
    {
        { IngestState.Queued },
        { IngestState.InProcess },
    };

    public static TheoryData<IngestState> FinalStates => new()
    {
        { IngestState.Completed },
        { IngestState.Failed },
        { IngestState.Interrupted },
    };
}
