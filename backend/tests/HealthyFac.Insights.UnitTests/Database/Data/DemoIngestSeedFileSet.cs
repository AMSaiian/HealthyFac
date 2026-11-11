using System.Text.Json;
using HealthyFac.Testing.Seeding;

namespace HealthyFac.Insights.UnitTests.Database.Data;

/// <summary>
/// Reads the committed demo ingest files <c>backend/seed/demo-ingest</c> for the consistency tests. The folder is
/// found next to <c>seed.Development.json</c>.
/// </summary>
public sealed class DemoIngestSeedFileSet : IDisposable
{
    private const string FolderName = "demo-ingest";
    private const string FilesFolderName = "files";

    private readonly JsonDocument _core;
    private readonly JsonDocument _document;
    private readonly JsonDocument _insights;
    private readonly JsonDocument _embeddings;

    public DemoIngestSeedFileSet()
    {
        Folder = Path.Combine(Path.GetDirectoryName(SeedDataFile.Find())!, FolderName);
        _core = Read("core.json");
        _document = Read("document.json");
        _insights = Read("insights.json");
        _embeddings = Read("insights-embeddings.json");
    }

    public string Folder { get; }

    public JsonElement Core => _core.RootElement;

    public JsonElement Document => _document.RootElement;

    public JsonElement Insights => _insights.RootElement;

    public JsonElement Embeddings => _embeddings.RootElement;

    public IReadOnlyList<JsonElement> AllRoots => [Core, Document, Insights, Embeddings];

    public IReadOnlyList<JsonElement> Rows(JsonElement root, string name) =>
        root.GetProperty(name).EnumerateArray().ToList();

    public string FilePath(string fileName, string extension) =>
        Path.Combine(Folder, FilesFolderName, $"{fileName}.{extension}");

    public void Dispose()
    {
        _core.Dispose();
        _document.Dispose();
        _insights.Dispose();
        _embeddings.Dispose();
    }

    private JsonDocument Read(string fileName) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, fileName)));
}
