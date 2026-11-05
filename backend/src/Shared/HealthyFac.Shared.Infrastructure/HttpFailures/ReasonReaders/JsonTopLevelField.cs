using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

/// <summary>
/// A scalar property of the root object of a JSON text. A reader of an error format checks the kind of the value
/// before it uses the text.
/// </summary>
public sealed record JsonTopLevelField
{
    public required JsonValueKind Kind { get; init; }

    /// <summary>
    /// The unescaped value of a string, the raw JSON text of a number (e.g., <c>1.5e3</c>, so no digit is lost), or
    /// <c>true</c> or <c>false</c> for a boolean.
    /// </summary>
    public required string Text { get; init; }
}
