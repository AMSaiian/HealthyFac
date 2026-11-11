namespace HealthyFac.Insights.Application.Files;

/// <summary>
/// Reads file data from the service that owns the files (Document).
/// </summary>
public interface IFileCatalog
{
    /// <summary>
    /// Reads a file on behalf of the user of the current request. Document scopes the read to the company of the
    /// user.
    /// </summary>
    /// <param name="fileId">The file to read.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The file, or <see langword="null"/> when Document has no such file for the company.</returns>
    Task<IngestFile?> GetFileForUserAsync(Guid fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a file and a signed link as the service, for the actor of the current flow. Document scopes the read to
    /// the company of the actor.
    /// </summary>
    /// <param name="fileId">The file to read.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>
    /// The file with its link, or <see langword="null"/> when Document has no such file for the company.
    /// </returns>
    Task<IngestFileLink?> GetFileLinkForWorkerAsync(Guid fileId, CancellationToken cancellationToken);
}
