using System.Text.Json.Serialization;

namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>The root of <c>document.json</c>.</summary>
public sealed record DemoDocumentSeedFile
{
    [JsonPropertyName("referenceTime")]
    public required DateTimeOffset ReferenceTime { get; init; }

    [JsonPropertyName("fileRefs")]
    public required IReadOnlyList<DemoFileSeedRow> FileRefs { get; init; }
}
