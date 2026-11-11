using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// Defines why an attempt was interrupted. The camelCase values reach the API and the database, and the web app maps
/// each to a translated text, so never rename one in place.
/// </summary>
public sealed class IngestInterruptReason : Enumeration<IngestInterruptReason>
{
    public static readonly IngestInterruptReason Cancelled = new(value: "cancelled");
    public static readonly IngestInterruptReason LeaseExpired = new(value: "leaseExpired");
    public static readonly IngestInterruptReason ServiceStopped = new(value: "serviceStopped");
    public static readonly IngestInterruptReason FileDeleted = new(value: "fileDeleted");

    private IngestInterruptReason(string value)
        : base(value)
    {
    }
}
