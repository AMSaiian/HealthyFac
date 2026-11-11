namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the failures of the demo file seed.
/// </summary>
public static class DemoFileSeedErrors
{
    public const string SeedFileEmpty = "The demo document seed file is empty.";

    public static string UnsupportedExtension(Guid fileId) => $"The demo file {fileId} has an unsupported extension.";
}
