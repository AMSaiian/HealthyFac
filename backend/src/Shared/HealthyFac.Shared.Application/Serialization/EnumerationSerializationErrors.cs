using System.Text.Json;

namespace HealthyFac.Shared.Application.Serialization;

/// <summary>
/// Defines error factories for the JSON serialization of enumeration members.
/// </summary>
public static class EnumerationSerializationErrors
{
    public static InvalidOperationException NotAnEnumeration(Type type) =>
        new($"{type.Name} does not derive from Enumeration.");

    public static JsonException UnknownEnumerationValue(Type type, string? value) =>
        new($"'{value}' is not a known {type.Name} value.");
}
