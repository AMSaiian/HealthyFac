using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.ApiTests.Ingest.Data;

public static class StartIngestAttemptTestData
{
    public const string WritePermission = "ingest:write";
    public const string ReadPermission = "ingest:read";
    public const string Pdf = "pdf";
    public const string Png = "png";
    public const string Attachment = "attachment";
    public const string Embedded = "embedded";

    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");

    public static readonly DateTime ChangedAt = new(
        year: 2026, month: 11, day: 9, hour: 8, minute: 30, second: 0, DateTimeKind.Utc);

    public static string StartPath(string encryptedFileId) => $"/v1/files/{encryptedFileId}/ingest-attempts";

    /// <summary>
    /// The earlier attempts of a file that refuse a new start, each with the stable error key of status 409.
    /// </summary>
    public static TheoryData<IngestState[], string> RefusedHistories => new()
    {
        { [IngestState.Queued], IngestErrors.IngestActive },
        { [IngestState.InProcess], IngestErrors.IngestActive },
        { [IngestState.Completed], IngestErrors.IngestCompleted },
        { [IngestState.Failed, IngestState.Failed, IngestState.Failed], IngestErrors.IngestFailedLimit },
    };

    /// <summary>
    /// The earlier attempts of a file that still allow a new start: fewer than 3 failed attempts, and any number of
    /// interrupted ones.
    /// </summary>
    public static TheoryData<IngestState[]> AllowedHistories => new()
    {
        { [] },
        { [IngestState.Failed, IngestState.Failed, IngestState.Interrupted, IngestState.Interrupted] },
    };
}
