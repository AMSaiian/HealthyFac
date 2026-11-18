namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the failures of the demo definition draft seed.
/// </summary>
public static class DemoDefinitionDraftSeedErrors
{
    public const string SeedFileEmpty = "The demo definition draft seed file is empty.";
    public const string EntryWithoutTarget = "A demo draft entry names neither a machine nor a machine draft.";
}
