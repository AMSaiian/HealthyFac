using System.Reflection;
using System.Text.RegularExpressions;
using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Finds integration messages that live outside a <c>HealthyFac.&lt;Service&gt;.Messaging</c> assembly or that more
/// than one assembly declares.
/// </summary>
internal static class MessageContractInspection
{
    private static readonly Regex MessagingAssemblyName = new(ConventionConstants.Messaging.AssemblyNamePattern);

    internal static IEnumerable<string> FindOutsideMessagingAssembly(IEnumerable<Assembly> assemblies) =>
        GetMessages(assemblies)
            .Where(type => !MessagingAssemblyName.IsMatch(type.Assembly.GetName().Name ?? string.Empty))
            .Select(type => $"{type.DisplayName} is in {type.Assembly.GetName().Name}");

    internal static IEnumerable<string> FindDuplicates(IEnumerable<Assembly> assemblies)
    {
        var messages = GetMessages(assemblies).ToList();
        var sameMessageType = messages
            .GroupBy(GetMessageType, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"MessageType '{group.Key}' is declared by {string.Join(separator: ", ", group.Select(Describe))}");
        var sameName = messages
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"Type name '{group.Key}' is declared by {string.Join(separator: ", ", group.Select(Describe))}");

        return sameMessageType.Concat(sameName);
    }

    internal static IEnumerable<string> FindDisallowedReferences(IEnumerable<Assembly> assemblies) =>
        assemblies
            .Where(assembly => MessagingAssemblyName.IsMatch(assembly.GetName().Name ?? string.Empty))
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => name.StartsWith(ConventionConstants.ProjectAssemblyPrefix, StringComparison.Ordinal))
                .Where(name => !ConventionConstants.Messaging.AllowedProjectReferences.Contains(name))
                .Select(name => $"{assembly.GetName().Name} references {name}"));

    private static IEnumerable<Type> GetMessages(IEnumerable<Assembly> assemblies) =>
        assemblies.SelectMany(assembly => assembly.GetSourceTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IIntegrationMessage).IsAssignableFrom(type));

    private static string GetMessageType(Type type) =>
        type.GetProperty(nameof(IIntegrationMessage.MessageType), BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(obj: null) as string
        ?? type.DisplayName;

    private static string Describe(Type type) => type.DisplayName;
}
