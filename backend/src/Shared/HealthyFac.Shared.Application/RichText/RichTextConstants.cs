namespace HealthyFac.Shared.Application.RichText;

/// <summary>
/// Defines the limits that every rich text field shares.
/// </summary>
public static class RichTextConstants
{
    /// <summary>
    /// The safety cap of the stored HTML string, in UTF-16 characters. It protects the storage and the HTML parser.
    /// The visible text limit of each field is lower and stays in the feature. The frontend mirrors the value in
    /// <c>shared/rich-text/rich-text-limits.ts</c>.
    /// </summary>
    public const int HtmlMaxLength = 20000;
}
