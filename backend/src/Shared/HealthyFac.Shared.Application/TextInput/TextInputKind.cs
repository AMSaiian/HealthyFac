namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// The form of a string input. A validator picks it with <c>SingleLineText()</c> or <c>MultilineText()</c>.
/// </summary>
public enum TextInputKind
{
    SingleLine,
    Multiline,
}
