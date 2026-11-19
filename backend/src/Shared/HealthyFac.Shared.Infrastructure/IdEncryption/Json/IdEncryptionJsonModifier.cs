using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Shared.Infrastructure.IdEncryption.Json;

/// <summary>
/// Adds the ID encryption converters to a JSON type info. The HTTP options of a service and the agent options of the
/// Insights service use the one modifier, so an ID looks the same on every boundary.
/// </summary>
public static class IdEncryptionJsonModifier
{
    /// <summary>
    /// Applies ID encryption converters to JSON object properties matching the ID naming convention: a
    /// <see cref="Guid"/> or nullable <see cref="Guid"/> named <c>id</c> or <c>*Id</c>, and a <see cref="Guid"/>
    /// collection named <c>*Ids</c> (an array, <see cref="List{T}"/>, or an interface that a list implements).
    /// </summary>
    /// <param name="typeInfo">The type info that the serializer builds.</param>
    /// <param name="encryptor">Encrypts and decrypts the IDs.</param>
    public static void Apply(JsonTypeInfo typeInfo, IIdEncryptor encryptor)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (!property.Name.EndsWith(
                    IdEncryptionConstants.Conventions.PropertySuffix,
                    StringComparison.OrdinalIgnoreCase)
                && !property.Name.EndsWith(
                    IdEncryptionConstants.Conventions.CollectionPropertySuffix,
                    StringComparison.OrdinalIgnoreCase)
                && !property.Name.Equals(
                    IdEncryptionConstants.Conventions.ExactPropertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hasIgnore = property.AttributeProvider?
                    .GetCustomAttributes(typeof(IgnoreIdEncryptionAttribute), inherit: false)
                    .Length
                > 0;

            if (hasIgnore)
            {
                continue;
            }

            var converter = ResolveConverter(property.PropertyType, encryptor);
            if (converter is not null)
            {
                property.CustomConverter = converter;
            }
        }
    }

    private static JsonConverter? ResolveConverter(Type propertyType, IIdEncryptor encryptor)
    {
        return propertyType switch
        {
            not null when propertyType == typeof(Guid) => new EncryptedGuidConverter(encryptor),
            not null when propertyType == typeof(Guid?) => new EncryptedNullableGuidConverter(encryptor),
            not null when IsGuidCollection(propertyType) => (JsonConverter?)Activator.CreateInstance(
                typeof(EncryptedGuidCollectionConverter<>).MakeGenericType(propertyType),
                encryptor),
            _ => null,
        };
    }

    private static bool IsGuidCollection(Type propertyType)
        => propertyType == typeof(Guid[])
            || (typeof(IEnumerable<Guid>).IsAssignableFrom(propertyType)
                && propertyType.IsAssignableFrom(typeof(List<Guid>)));
}
