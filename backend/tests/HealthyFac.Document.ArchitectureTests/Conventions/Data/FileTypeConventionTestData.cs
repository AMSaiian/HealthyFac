namespace HealthyFac.Document.ArchitectureTests.Conventions.Data;

public static class FileTypeConventionTestData
{
    /// <summary>The file types that have no binary signature. UTF-8 text decides them.</summary>
    public static readonly IReadOnlyList<string> TextExtensions = ["txt", "csv", "md"];

    /// <summary>A format type whose name holds this text allows macros.</summary>
    public const string MacroMarker = "Macros";
}
