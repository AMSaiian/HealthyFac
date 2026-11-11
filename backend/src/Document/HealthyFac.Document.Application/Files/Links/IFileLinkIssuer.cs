using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Links;

/// <summary>
/// Issues the read link of a file. The Infrastructure layer signs the link with the storage key.
/// </summary>
public interface IFileLinkIssuer
{
    /// <summary>
    /// Signs a read link for the blob of a file. The call makes no storage request.
    /// </summary>
    /// <param name="file">The file ref. Its stored type and name decide the response headers of the link.</param>
    /// <param name="audience">Who reads the link. It picks the host of the link.</param>
    /// <returns>The link and the time that it expires.</returns>
    FileLink Issue(FileRef file, FileLinkAudience audience);
}
