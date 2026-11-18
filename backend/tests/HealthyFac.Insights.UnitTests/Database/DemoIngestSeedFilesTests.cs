using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using HealthyFac.Insights.UnitTests.Database.Data;

namespace HealthyFac.Insights.UnitTests.Database;

/// <summary>
/// Checks the committed demo ingest files as one snapshot: the counts, the references between the files, the vectors,
/// the file hashes, and the absence of company, user, and audit values.
/// </summary>
public sealed class DemoIngestSeedFilesTests : IDisposable
{
    private const int FileRefCount = 3;
    private const int AttemptCount = 3;
    private const int IngestedDocumentCount = 3;
    private const int ChunkCount = 101;
    private const int MachineDraftCount = 3;
    private const int MetricDraftCount = 40;
    private const int StatusDraftCount = 84;
    private const int VectorDimensions = 1536;
    private const double NormTolerance = 1e-5;

    private static readonly string[] ForbiddenKeys =
        ["company_id", "created_by", "updated_by", "deleted_by", "created_at", "updated_at", "deleted_at"];

    private readonly DemoIngestSeedFileSet _files = new();

    [Fact]
    public void Files_Always_HoldTheSnapshotCounts()
    {
        var counts = new[]
        {
            _files.Rows(_files.Document, name: "fileRefs").Count,
            _files.Rows(_files.Insights, name: "ingestAttempts").Count,
            _files.Rows(_files.Insights, name: "ingestedDocuments").Count,
            _files.Rows(_files.Insights, name: "documentChunks").Count,
            _files.Rows(_files.Core, name: "machineDrafts").Count,
            _files.Rows(_files.Telemetry, name: "metricDefinitionDrafts").Count,
            _files.Rows(_files.Telemetry, name: "statusDefinitionDrafts").Count,
        };

        counts.Should()
            .Equal(
                FileRefCount,
                AttemptCount,
                IngestedDocumentCount,
                ChunkCount,
                MachineDraftCount,
                MetricDraftCount,
                StatusDraftCount);
    }

    [Fact]
    public void Drafts_Always_ReferenceAnExistingAttemptAndFile()
    {
        var attemptIds = Ids(_files.Rows(_files.Insights, name: "ingestAttempts"));
        var fileIds = Ids(_files.Rows(_files.Document, name: "fileRefs"));
        var drafts = _files.Rows(_files.Core, name: "machineDrafts")
            .Concat(_files.Rows(_files.Telemetry, name: "metricDefinitionDrafts"))
            .Concat(_files.Rows(_files.Telemetry, name: "statusDefinitionDrafts"))
            .ToList();

        var unknownAttempts = drafts.Select(draft => Text(draft, name: "attempt_id"))
            .Where(id => !attemptIds.Contains(id));
        var unknownFiles = drafts.Select(draft => Text(draft, name: "file_id")).Where(id => !fileIds.Contains(id));

        unknownAttempts.Should().BeEmpty();
        unknownFiles.Should().BeEmpty();
    }

    [Fact]
    public void CoreFile_Always_HoldsNoMetricOrStatusDraftRows()
    {
        var names = _files.Core.EnumerateObject().Select(property => property.Name);

        names.Should().BeEquivalentTo("referenceTime", "machineDrafts");
    }

    [Fact]
    public void DefinitionDrafts_Always_NameTheMachineDraftOfTheirProposalEntry()
    {
        var machineDraftIds = Ids(_files.Rows(_files.Core, name: "machineDrafts"));
        var entryIds = _files.Rows(_files.Telemetry, name: "metricDefinitionDrafts")
            .Concat(_files.Rows(_files.Telemetry, name: "statusDefinitionDrafts"))
            .Select(draft => Text(draft, name: "proposal_entry_id"));

        entryIds.Where(id => !machineDraftIds.Contains(id)).Should().BeEmpty();
    }

    [Fact]
    public void Attempts_Always_ReferenceAnExistingFile()
    {
        var fileIds = Ids(_files.Rows(_files.Document, name: "fileRefs"));
        var attempts = _files.Rows(_files.Insights, name: "ingestAttempts");

        attempts.Select(attempt => Text(attempt, name: "file_id"))
            .Where(id => !fileIds.Contains(id))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Chunks_Always_ReferenceAnExistingDocument()
    {
        var documentIds = Ids(_files.Rows(_files.Insights, name: "ingestedDocuments"));
        var chunks = _files.Rows(_files.Insights, name: "documentChunks");

        chunks.Select(chunk => Text(chunk, name: "document_id"))
            .Where(id => !documentIds.Contains(id))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Embeddings_Always_MatchTheChunksOneToOne()
    {
        var chunkIds = Ids(_files.Rows(_files.Insights, name: "documentChunks"));
        var embeddingKeys = _files.Embeddings.GetProperty(propertyName: "embeddings")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet();

        embeddingKeys.Should().BeEquivalentTo(chunkIds);
    }

    [Fact]
    public void Embeddings_Always_DecodeToUnitVectorsOf1536Values()
    {
        var vectors = _files.Embeddings.GetProperty(propertyName: "embeddings")
            .EnumerateObject()
            .Select(property => Describe(property.Value.GetString()!))
            .ToList();

        vectors.Should().OnlyContain(vector => vector.Length == VectorDimensions);
        vectors.Should().OnlyContain(vector => Math.Abs(vector.Norm - 1) <= NormTolerance);
    }

    [Fact]
    public void FileRefs_Always_HaveTheSha256OfTheirFile()
    {
        var mismatches = _files.Rows(_files.Document, name: "fileRefs")
            .Where(row => Text(row, name: "sha256")
                != Sha256(
                    _files.FilePath(Text(row, name: "file_name"), Text(row, name: "extension"))))
            .Select(row => Text(row, name: "file_name"));

        mismatches.Should().BeEmpty();
    }

    [Fact]
    public void Files_Always_HoldNoCompanyUserOrAuditKey()
    {
        var keys = _files.AllRoots.SelectMany(PropertyNames).Where(ForbiddenKeys.Contains).Distinct();

        keys.Should().BeEmpty();
    }

    [Fact]
    public void RowFiles_Always_HoldTheSameReferenceTime()
    {
        var times = new[] { _files.Core, _files.Telemetry, _files.Document, _files.Insights }
            .Select(root => root.GetProperty(propertyName: "referenceTime").GetString())
            .Distinct();

        times.Should().ContainSingle();
    }

    public void Dispose() => _files.Dispose();

    private static HashSet<string> Ids(IEnumerable<JsonElement> rows) =>
        rows.Select(row => Text(row, name: "id")).ToHashSet();

    private static string Text(JsonElement row, string name) => row.GetProperty(name).GetString()!;

    private static (int Length, double Norm) Describe(string base64)
    {
        var vector = MemoryMarshal.Cast<byte, float>(Convert.FromBase64String(base64));
        double sum = 0;

        foreach (var value in vector)
        {
            sum += (double)value * value;
        }

        return (vector.Length, Math.Sqrt(sum));
    }

    private static string Sha256(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private static IEnumerable<string> PropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;

                    foreach (var name in PropertyNames(property.Value))
                    {
                        yield return name;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var name in PropertyNames(item))
                    {
                        yield return name;
                    }
                }

                break;
        }
    }
}
