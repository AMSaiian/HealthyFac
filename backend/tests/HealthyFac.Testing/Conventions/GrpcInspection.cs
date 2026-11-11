using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grpc.Core;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Finds gRPC services without the shared port setup and contract assemblies that reference a service project.
/// </summary>
internal static class GrpcInspection
{
    private static readonly Regex ContractsAssemblyName = new(ConventionConstants.Grpc.ContractsAssemblyNamePattern);

    internal static IEnumerable<string> FindServicesWithoutPortSetup(IEnumerable<Assembly> assemblies) =>
        assemblies
            .Where(assembly => assembly.GetSourceTypes().Any(IsGrpcService))
            .SelectMany(FindMissingSetup);

    internal static IEnumerable<string> FindContractReferences(IEnumerable<Assembly> assemblies) =>
        assemblies
            .Where(assembly => ContractsAssemblyName.IsMatch(assembly.GetName().Name ?? string.Empty))
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => name.StartsWith(ConventionConstants.ProjectAssemblyPrefix, StringComparison.Ordinal))
                .Select(name => $"{assembly.GetName().Name} references {name}"));

    private static bool IsGrpcService(Type type) =>
        type is { IsClass: true, IsAbstract: false, BaseType: not null }
        && type.BaseType.GetCustomAttribute<BindServiceMethodAttribute>() is not null;

    private static IEnumerable<string> FindMissingSetup(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        var isSharedWebReferenced = assembly.GetReferencedAssemblies()
            .Any(reference => reference.Name == ConventionConstants.Grpc.SharedWebAssemblyName);
        if (!isSharedWebReferenced)
        {
            yield return $"{name} serves gRPC without a reference to {ConventionConstants.Grpc.SharedWebAssemblyName}";
        }

        var settingsPath = Path.Combine(
            Path.GetDirectoryName(assembly.Location) ?? string.Empty,
            ConventionConstants.Grpc.SettingsFileName);
        var hasPort = File.Exists(settingsPath)
            && JsonDocument.Parse(File.ReadAllText(settingsPath))
                .RootElement
                .TryGetProperty(ConventionConstants.Grpc.SettingsSection, out var section)
            && section.TryGetProperty(ConventionConstants.Grpc.SettingsPort, out _);
        if (!hasPort)
        {
            yield return $"{name} serves gRPC without {ConventionConstants.Grpc.SettingsSection}:"
                + $"{ConventionConstants.Grpc.SettingsPort} in {ConventionConstants.Grpc.SettingsFileName}";
        }
    }
}
