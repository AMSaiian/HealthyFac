namespace HealthyFac.Insights.Infrastructure.Conversion;

public static class DoclingMessages
{
    public const string BaseUrlInvalid = "Docling:BaseUrl must be an absolute http or https URI.";
    public const string ApiKeyRequired = "Docling:ApiKey is required.";
    public const string PollSecondsInvalid = "Docling:PollSeconds must be above 0.";
    public const string MaxPagesInvalid = "Docling:MaxPages must be above 0.";
}
