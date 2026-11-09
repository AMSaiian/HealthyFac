using System.Reflection;

namespace HealthyFac.Document.Application;

/// <summary>
/// Labels the Document Application assembly for assembly scans (use cases, decorators, validators). A scan reads the
/// assembly through this class, so moving or renaming a business type never changes what the scan finds.
/// </summary>
public static class DocumentApplicationAssembly
{
    public static readonly Assembly Instance = typeof(DocumentApplicationAssembly).Assembly;
}
