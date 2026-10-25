using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Web.Serialization;

namespace HealthyFac.Shared.Web.IdEncryption.Converters;

/// <summary>
/// Reads and writes a collection of <see cref="Guid"/> values as a JSON array of encrypted identifier tokens. Each
/// element follows <see cref="EncryptedGuidConverter"/>. Reading builds a <see cref="List{T}"/>, so
/// <typeparamref name="TCollection"/> must be an array of <see cref="Guid"/> or a type that a list of
/// <see cref="Guid"/> implements (e.g., <see cref="IReadOnlyList{T}"/>).
/// </summary>
/// <typeparam name="TCollection">The declared type of the collection property.</typeparam>
/// <param name="encryptor">The encryptor for each element.</param>
public sealed class EncryptedGuidCollectionConverter<TCollection>(IIdEncryptor encryptor) : JsonConverter<TCollection>
    where TCollection : IEnumerable<Guid>
{
    private readonly EncryptedGuidConverter _elementConverter = new(encryptor);

    /// <inheritdoc/>
    /// <exception cref="JsonException">Thrown when the value is not an array, or when an element is not a valid
    /// encrypted identifier.</exception>
    public override TCollection Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw SerializationErrors.ExpectedEncryptedIdentifierArray();
        }

        var ids = new List<Guid>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ids.Add(_elementConverter.Read(ref reader, typeof(Guid), options));
        }

        return typeof(TCollection) == typeof(Guid[])
            ? (TCollection)(object)ids.ToArray()
            : (TCollection)(object)ids;
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        TCollection value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();

        foreach (var id in value)
        {
            _elementConverter.Write(writer, id, options);
        }

        writer.WriteEndArray();
    }
}
