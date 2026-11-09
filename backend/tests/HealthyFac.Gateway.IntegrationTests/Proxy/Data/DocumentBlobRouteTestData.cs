using System.Net;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class DocumentBlobRouteTestData
{
    public const string BlobPathFormat = "/api/document/blobs/files/{0}?{1}";
    public const string PdfType = "application/pdf";
    public const string AttachmentDisposition = "attachment; filename=\"report.pdf\"";
    public const string SignatureMarker = "sig=";
    public const string ServerSpanKind = "Server";
    public const string AuthorizationHeader = "Authorization";
    public const string CookieHeader = "Cookie";
    public const string CookieValue = "session=secret";
    public const string StaleUpdateBody = "changed";
    public const string TracingEnabledKey = "Tracing:IsEnabled";
    public const string TracingEnabledValue = "true";

    public static readonly byte[] Content = [.. "%PDF-1.7 blob"u8];

    /// <summary>
    /// A write method matches no blob route. The anonymous request then meets the fallback policy, so the answer is
    /// <c>401</c> for every method.
    /// </summary>
    public static TheoryData<string, HttpStatusCode> WriteMethods => new()
    {
        { "PUT", HttpStatusCode.Unauthorized },
        { "DELETE", HttpStatusCode.Unauthorized },
        { "POST", HttpStatusCode.Unauthorized },
    };
}
