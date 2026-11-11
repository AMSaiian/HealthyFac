namespace HealthyFac.Insights.Application.Conversion;

/// <summary>
/// Converts a file to text chunks (OCR, table structure, and chunking by a conversion service).
/// </summary>
public interface IDocumentConverter
{
    /// <summary>
    /// Converts a file. The method streams the content to the service and holds no copy of it. Each external call
    /// of the conversion is one retry unit: the open of the content with the upload, each poll, and the read of the
    /// result.
    /// </summary>
    /// <param name="openContent">
    /// Opens the content of the file. The method calls it again when it repeats the upload, and it disposes the stream.
    /// </param>
    /// <param name="fileName">The name of the file without the extension.</param>
    /// <param name="extension">The lower case extension of the file without the dot.</param>
    /// <param name="isCancelled">Tells whether the user cancelled. The method asks before each poll.</param>
    /// <param name="cancellationToken">The token that cancels the call (a stop of the service or a timeout).</param>
    /// <returns>The page count and the chunks.</returns>
    /// <exception cref="ConversionFailedException">Thrown when the service reports a failed conversion.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled or <paramref name="isCancelled"/> returns true.
    /// </exception>
    Task<ConvertedDocument> ConvertAsync(
        Func<CancellationToken, Task<Stream>> openContent,
        string fileName,
        string extension,
        Func<CancellationToken, Task<bool>> isCancelled,
        CancellationToken cancellationToken
    );
}
