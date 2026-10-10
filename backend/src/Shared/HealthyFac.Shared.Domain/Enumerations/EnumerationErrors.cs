namespace HealthyFac.Shared.Domain.Enumerations;

/// <summary>
/// Defines error factories for enumeration lookups.
/// </summary>
public static class EnumerationErrors
{
    public static InvalidOperationException UnknownValue(Type enumerationType, string value) =>
        new($"'{value}' is not a known {enumerationType.Name} value.");

    public static ArgumentException ValueRequired(string parameterName) =>
        new("An enumeration value cannot be null, empty, or whitespace.", parameterName);
}
