using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

internal static class JsonReasonFields
{
    /// <summary>
    /// Reads a string field of an error body. A number or a boolean gives no text, because the standard formats
    /// define their reason fields as strings.
    /// </summary>
    /// <param name="fields">The scalar fields of the root object of the error body.</param>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>The text, or <see langword="null"/> when the field is missing, is not a string, or is blank.</returns>
    public static string? ReadText(IReadOnlyDictionary<string, JsonTopLevelField> fields, string fieldName) =>
        fields.TryGetValue(fieldName, out var field)
        && field.Kind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(field.Text)
            ? field.Text
            : null;
}
