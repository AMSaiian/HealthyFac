namespace HealthyFac.Shared.Infrastructure.RichText;

/// <summary>
/// Defines the attribute names and the class value of the rich text allow-list.
/// </summary>
internal static class RichTextAttributes
{
    public const string Href = "href";
    public const string Target = "target";
    public const string Rel = "rel";
    public const string Class = "class";
    public const string DataList = "data-list";
    public const string ContentEditable = "contenteditable";

    /// <summary>The class of the list marker element that Quill 2 adds inside each list item.</summary>
    public const string QuillUiClass = "ql-ui";
}
