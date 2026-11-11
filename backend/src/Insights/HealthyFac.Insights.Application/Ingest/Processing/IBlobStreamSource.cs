namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Opens the content of a file from a signed link as a stream.
/// </summary>
public interface IBlobStreamSource
{
    /// <summary>
    /// Opens the stream. The stream reads from the network, so the caller never holds the whole file.
    /// </summary>
    /// <param name="url">The signed link of the file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The stream. The caller disposes it.</returns>
    Task<Stream> OpenAsync(string url, CancellationToken cancellationToken);
}
