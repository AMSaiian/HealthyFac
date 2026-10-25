using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Web.Serialization;

namespace HealthyFac.Shared.Web.IdEncryption.Converters;

/// <summary>
/// Reads and writes a <see cref="Guid"/> as an encrypted identifier token.
/// An absent or empty token reads as <see cref="Guid.Empty"/>, which request validation then rejects.
/// </summary>
public sealed class EncryptedGuidConverter(IIdEncryptor encryptor) : JsonConverter<Guid>
{
    /// <inheritdoc/>
    /// <exception cref="JsonException">Thrown when the token is not a valid encrypted identifier.</exception>
    public override Guid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var encrypted = reader.GetString();
        if (string.IsNullOrEmpty(encrypted))
        {
            return Guid.Empty;
        }

        try
        {
            return encryptor.Decrypt(encrypted);
        }
        catch (Exception exception) when (exception is FormatException
            or CryptographicException
            or ArgumentException)
        {
            throw SerializationErrors.InvalidEncryptedIdentifier(exception);
        }
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        Guid value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(encryptor.Encrypt(value));
}
