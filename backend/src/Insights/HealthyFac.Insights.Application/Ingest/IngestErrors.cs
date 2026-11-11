namespace HealthyFac.Insights.Application.Ingest;

/// <summary>
/// Defines error messages for the ingest use cases.
/// </summary>
public static class IngestErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string FileIdRequired = "The file is required.";
    public const string FileNotFound = "The file does not exist.";
    public const string AttemptIdRequired = "The ingest attempt is required.";
    public const string AttemptNotFound = "The ingest attempt does not exist.";

    /// <summary>The name of the field that a validation failure of the file type reports.</summary>
    public const string FileTypeField = "extension";

    /// <summary>
    /// The stable key that the web app maps to a translated text. The start returns it with status 400 when the
    /// extension of the file is not ingestable.
    /// </summary>
    public const string FileTypeNotIngestable = "fileTypeNotIngestable";

    /// <summary>The stable key of status 409 when the file has a queued or running attempt.</summary>
    public const string IngestActive = "ingestActive";

    /// <summary>The stable key of status 409 when the file has a completed attempt.</summary>
    public const string IngestCompleted = "ingestCompleted";

    /// <summary>The stable key of status 409 when the file reached the limit of failed attempts.</summary>
    public const string IngestFailedLimit = "ingestFailedLimit";

    /// <summary>The stable key of status 409 when the attempt is final and cannot be cancelled.</summary>
    public const string IngestFinal = "ingestFinal";

    public const string IngestChanged = "The ingest attempt changed. Reload it and try again.";
}
