namespace HealthyFac.Shared.Infrastructure.Seeding;

/// <summary>
/// Holds the folder of the demo ingest snapshot. The default is <see cref="DemoIngestSeedFiles.RelativeFolder"/> in
/// the application base. A test passes the folder of the committed files instead.
/// </summary>
/// <param name="folder">The absolute path of the snapshot folder.</param>
public sealed class DemoIngestSeedLocation(string folder)
{
    /// <summary>The absolute path of the snapshot folder.</summary>
    public string Folder { get; } = folder;

    /// <summary>
    /// Builds the default location.
    /// </summary>
    /// <returns>The folder under the application base.</returns>
    public static DemoIngestSeedLocation FromApplicationBase() =>
        new(Path.Combine(AppContext.BaseDirectory, DemoIngestSeedFiles.RelativeFolder));
}
