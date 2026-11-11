using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// Defines why an attempt failed. The camelCase values reach the API and the database, and the web app maps each to a
/// translated text, so never rename one in place.
/// </summary>
public sealed class IngestErrorCode : Enumeration<IngestErrorCode>
{
    public static readonly IngestErrorCode FileNotFound = new(value: "fileNotFound");
    public static readonly IngestErrorCode NoText = new(value: "noText");
    public static readonly IngestErrorCode TooManyPages = new(value: "tooManyPages");
    public static readonly IngestErrorCode ConversionFailed = new(value: "conversionFailed");
    public static readonly IngestErrorCode ConversionTimeout = new(value: "conversionTimeout");
    public static readonly IngestErrorCode InvalidExtraction = new(value: "invalidExtraction");
    public static readonly IngestErrorCode ProviderUnavailable = new(value: "providerUnavailable");

    private IngestErrorCode(string value)
        : base(value)
    {
    }
}
