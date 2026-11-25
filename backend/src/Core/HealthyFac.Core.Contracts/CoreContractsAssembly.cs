using System.Reflection;

namespace HealthyFac.Core.Contracts;

/// <summary>
/// Labels the Core Contracts assembly for assembly scans (architecture tests, convention checks).
/// </summary>
public static class CoreContractsAssembly
{
    public static readonly Assembly Instance = typeof(CoreContractsAssembly).Assembly;
}
