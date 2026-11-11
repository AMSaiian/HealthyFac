using HealthyFac.Insights.Application.Files;

namespace HealthyFac.Insights.ApiTests.Hosting;

/// <summary>
/// Replaces the Document gRPC catalog in the API tests. A test adds the files that Document would answer for. A file
/// that a test did not add is "not found".
/// </summary>
public sealed class FakeFileCatalog : IFileCatalog
{
    private const string SignedUrl = "https://files.test/signed";

    private readonly Dictionary<Guid, IngestFile> _files = [];

    public void Add(IngestFile file) => _files[file.FileId] = file;

    public void Clear() => _files.Clear();

    public Task<IngestFile?> GetFileForUserAsync(Guid fileId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.GetValueOrDefault(fileId));

    public Task<IngestFileLink?> GetFileLinkForWorkerAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var isKnown = _files.TryGetValue(fileId, out var file);

        return Task.FromResult(
            isKnown
                ? new IngestFileLink { File = file!, Url = SignedUrl, ExpiresAt = DateTime.UtcNow }
                : null);
    }
}
