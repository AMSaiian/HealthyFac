using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.IdEncryption.Json;

/// <summary>
/// Defines error factories for JSON serialization of domain values.
/// </summary>
public static class SerializationErrors
{
    /// <summary>
    /// Reports a malformed encrypted identifier. A <see cref="JsonException"/> makes the request binder
    /// answer 400, because a bad identifier in the payload is a client error, not a server fault.
    /// </summary>
    /// <param name="innerException">The decryption failure that caused this.</param>
    /// <returns>The exception to throw from a converter.</returns>
    public static JsonException InvalidEncryptedIdentifier(Exception innerException) =>
        new(message: "The value is not a valid encrypted identifier.", innerException);

    public static JsonException ExpectedEncryptedIdentifierArray() =>
        new(message: "The value is not an array of encrypted identifiers.");
}
