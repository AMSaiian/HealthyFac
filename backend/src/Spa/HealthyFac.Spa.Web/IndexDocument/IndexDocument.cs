namespace HealthyFac.Spa.Web.IndexDocument;

/// <summary>
/// Holds the text of <c>index.html</c>. The host reads the file once at startup and renders it per request with a
/// new nonce.
/// </summary>
public sealed class IndexDocument
{
    private readonly string _template;

    private IndexDocument(string template)
    {
        _template = template;
    }

    /// <summary>
    /// Reads the index file of the content root.
    /// </summary>
    /// <param name="contentRoot">The folder with the built app.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the folder holds no index file.</exception>
    public static IndexDocument Load(string contentRoot)
    {
        var path = Path.Combine(contentRoot, SpaConstants.IndexFileName);
        if (!File.Exists(path))
        {
            throw IndexDocumentErrors.IndexFileMissing(path);
        }

        return new IndexDocument(File.ReadAllText(path));
    }

    /// <summary>
    /// Replaces every nonce placeholder of the index file.
    /// </summary>
    /// <param name="nonce">The nonce of the current response.</param>
    /// <returns>The HTML text for the response.</returns>
    public string Render(string nonce) => _template.Replace(SpaConstants.NoncePlaceholder, nonce);
}
