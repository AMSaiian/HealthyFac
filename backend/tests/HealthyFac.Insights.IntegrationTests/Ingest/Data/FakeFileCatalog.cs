using HealthyFac.Insights.Application.Files;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Replaces the Document gRPC catalog in the integration tests. A test adds the files that Document would answer for.
/// A file that a test did not add is "not found".
/// </summary>
public sealed class FakeFileCatalog : IFileCatalog
{
    private const string SignedUrl = "https://files.test/signed";

    private readonly Dictionary<Guid, IngestFile> _files = [];

    public void Add(IngestFile file) => _files[file.FileId] = file;

    public int CallCount { get; private set; }

    /// <summary>When set, every request of a link throws it after it counts the call.</summary>
    public Exception? Failure { get; set; }

    public void Clear()
    {
        _files.Clear();
        CallCount = 0;
        Failure = null;
    }

    public Task<IngestFile?> GetFileForUserAsync(Guid fileId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.GetValueOrDefault(fileId));

    public Task<IngestFileLink?> GetFileLinkForWorkerAsync(Guid fileId, CancellationToken cancellationToken)
    {
        CallCount++;
        if (Failure is not null)
        {
            throw Failure;
        }

        var isKnown = _files.TryGetValue(fileId, out var file);

        return Task.FromResult(
            isKnown
                ? new IngestFileLink { File = file!, Url = SignedUrl, ExpiresAt = DateTime.UtcNow }
                : null);
    }
}
