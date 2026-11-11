namespace HealthyFac.Document.Application.Files.Links;

/// <summary>
/// Who reads a signed file link. The audience picks the host of the link. The signature is the same for both.
/// </summary>
public enum FileLinkAudience
{
    /// <summary>
    /// A link for a browser, through the gateway.
    /// </summary>
    Public,

    /// <summary>
    /// A link for one of our services on the private network, straight to the storage endpoint.
    /// </summary>
    Internal,
}
