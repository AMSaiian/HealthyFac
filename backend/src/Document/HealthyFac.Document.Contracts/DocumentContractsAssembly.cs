using System.Reflection;

namespace HealthyFac.Document.Contracts;

/// <summary>
/// Labels the Document Contracts assembly for assembly scans (architecture tests, convention checks).
/// </summary>
public static class DocumentContractsAssembly
{
    public static readonly Assembly Instance = typeof(DocumentContractsAssembly).Assembly;
}
