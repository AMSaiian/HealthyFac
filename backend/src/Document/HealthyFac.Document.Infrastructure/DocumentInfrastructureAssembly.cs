using System.Reflection;

namespace HealthyFac.Document.Infrastructure;

/// <summary>
/// Labels the Document Infrastructure assembly for assembly scans (entity configurations). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class DocumentInfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(DocumentInfrastructureAssembly).Assembly;
}
