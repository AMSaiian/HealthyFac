using System.Text.Json;

namespace HealthyFac.Shared.Web.Serialization;

/// <summary>
/// Defines error factories for JSON serialization of domain values.
/// </summary>
public static class SerializationErrors
{
    public static InvalidOperationException NotAnEnumeration(Type type) =>
        new($"{type.Name} does not derive from Enumeration.");

    /// <summary>
    /// Reports a malformed encrypted identifier. A <see cref="JsonException"/> makes the request binder
    /// answer 400, because a bad identifier in the payload is a client error, not a server fault.
    /// </summary>
    /// <param name="innerException">The decryption failure that caused this.</param>
    /// <returns>The exception to throw from a converter.</returns>
    public static JsonException InvalidEncryptedIdentifier(Exception innerException) =>
        new("The value is not a valid encrypted identifier.", innerException);

    public static JsonException ExpectedEncryptedIdentifierArray() =>
        new("The value is not an array of encrypted identifiers.");

    public static JsonException UnknownEnumerationValue(Type type, string? value) =>
        new($"'{value}' is not a known {type.Name} value.");
}
