using System.Collections.Frozen;
using System.Reflection;
using HealthyFac.Core.Web.Authorization;

namespace HealthyFac.Core.UnitTests.Authorization.Data;

public static class CorePermissionsTestData
{
    public static IReadOnlyList<PermissionFeature> Features { get; } = typeof(CorePermissions)
        .GetNestedTypes(BindingFlags.Public)
        .Select(feature => new PermissionFeature
        {
            Name = feature.Name,
            Constants = ReadConstants(feature),
            All = (FrozenSet<string>)feature.GetField(nameof(CorePermissions.All))!.GetValue(null)!,
        })
        .ToList();

    private static List<string> ReadConstants(Type feature) => feature
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToList();
}

public sealed record PermissionFeature
{
    public required string Name { get; init; }

    public required IReadOnlyCollection<string> Constants { get; init; }

    public required IReadOnlyCollection<string> All { get; init; }
}
