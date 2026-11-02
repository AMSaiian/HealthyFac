namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// The form of a string input, from the attribute of its property. A property without an attribute and every string
/// element of a collection are single-line.
/// </summary>
public enum TextInputKind
{
    SingleLine,
    Multiline,
    Raw,
}
