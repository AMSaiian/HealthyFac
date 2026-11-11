using System.Reflection;

namespace HealthyFac.Document.Messaging;

/// <summary>
/// Labels the Document Messaging assembly for assembly scans (architecture tests, convention checks).
/// A scan reads the assembly through this class, so moving or renaming a business type never changes what the
/// scan finds.
/// </summary>
public static class DocumentMessagingAssembly
{
    public static readonly Assembly Instance = typeof(DocumentMessagingAssembly).Assembly;
}
