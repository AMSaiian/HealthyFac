using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.Infrastructure.TextInput;

/// <summary>
/// Adds the text input normalization to JSON options. The API options and the messaging options both call it, so a
/// request and a message read the same text in the same way.
/// </summary>
public static class TextInputJson
{
    private static readonly TextInputJsonConverter SingleLine = new(TextInputKind.SingleLine);
    private static readonly TextInputJsonConverter Multiline = new(TextInputKind.Multiline);
    private static readonly TextInputJsonConverter Raw = new(TextInputKind.Raw);

    /// <summary>
    /// Adds a single-line converter for every string, which also covers the strings in collections and nested
    /// objects, and a resolver modifier that gives a property with <see cref="MultilineTextAttribute"/> or
    /// <see cref="RawTextAttribute"/> its own converter. A plain converter cannot see the attributes of a property.
    /// </summary>
    /// <param name="options">The options to change. They must not be read-only yet.</param>
    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(SingleLine);
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(ApplyPropertyForms);
    }

    private static void ApplyPropertyForms(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties.Where(property => property.PropertyType == typeof(string)))
        {
            property.CustomConverter = TextInputPolicy.KindOf(property.AttributeProvider) switch
            {
                TextInputKind.Multiline => Multiline,
                TextInputKind.Raw => Raw,
                _ => property.CustomConverter,
            };
        }
    }
}
