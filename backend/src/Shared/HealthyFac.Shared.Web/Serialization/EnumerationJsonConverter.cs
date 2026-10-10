using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Web.Serialization;

/// <summary>
/// Serializes an <see cref="Enumeration{TEnum}"/> member as its camelCase value, and reads it back.
/// </summary>
public sealed class EnumerationJsonConverter : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => FindEnumerationBase(typeToConvert) is not null;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="typeToConvert"/> does not derive from <see cref="Enumeration{TEnum}"/>.
    /// </exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumerationBase = FindEnumerationBase(typeToConvert)
            ?? throw SerializationErrors.NotAnEnumeration(typeToConvert);

        var memberType = enumerationBase.GetGenericArguments()[0];

        return (JsonConverter)Activator.CreateInstance(
            typeof(EnumerationValueConverter<>).MakeGenericType(memberType))!;
    }

    private static Type? FindEnumerationBase(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Enumeration<>))
            {
                return current;
            }
        }

        return null;
    }

    private sealed class EnumerationValueConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : Enumeration<TEnum>
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetString();

            return Enumeration<TEnum>.TryFromValue(value, out var member)
                ? member
                : throw SerializationErrors.UnknownEnumerationValue(typeof(TEnum), value);
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
