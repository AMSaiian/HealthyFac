using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Web.Serialization;

namespace HealthyFac.Shared.Web.IdEncryption.Converters;

/// <summary>
/// Reads and writes an optional <see cref="Guid"/> as an encrypted identifier token.
/// A null or empty token reads as <see langword="null"/>.
/// </summary>
public sealed class EncryptedNullableGuidConverter(IIdEncryptor encryptor) : JsonConverter<Guid?>
{
    /// <inheritdoc/>
    /// <exception cref="JsonException">Thrown when the token is not a valid encrypted identifier.</exception>
    public override Guid? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var encrypted = reader.GetString();
        if (string.IsNullOrEmpty(encrypted))
        {
            return null;
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
        Guid? value,
        JsonSerializerOptions options
    )
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        writer.WriteStringValue(encryptor.Encrypt(value.Value));
    }
}
