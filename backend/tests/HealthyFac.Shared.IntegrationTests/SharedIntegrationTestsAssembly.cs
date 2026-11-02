using System.Reflection;

namespace HealthyFac.Shared.IntegrationTests;

/// <summary>
/// Labels the Shared integration test assembly for assembly scans (test use cases). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class SharedIntegrationTestsAssembly
{
    public static readonly Assembly Instance = typeof(SharedIntegrationTestsAssembly).Assembly;
}
