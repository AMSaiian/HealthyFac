using System.Text.Json.Serialization;

namespace HealthyFac.Document.Infrastructure.Database.Seeding.DemoFiles;

/// <summary>The root of <c>document.json</c>.</summary>
public sealed record DemoDocumentSeedFile
{
    [JsonPropertyName(name: "referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName(name: "fileRefs")]
    public required IReadOnlyList<DemoFileSeedRow> FileRefs { get; init; }
}
