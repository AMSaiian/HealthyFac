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
    public const string DataFileId = "data-file-id";
    public const string Alt = "alt";

    /// <summary>The <c>data-list</c> value of a bullet list item.</summary>
    public const string DataListBullet = "bullet";

    /// <summary>The <c>data-list</c> value of an ordered list item.</summary>
    public const string DataListOrdered = "ordered";

    /// <summary>The <c>data-list</c> value of a checked checklist item.</summary>
    public const string DataListChecked = "checked";

    /// <summary>The <c>data-list</c> value of an unchecked checklist item.</summary>
    public const string DataListUnchecked = "unchecked";

    /// <summary>The class of the list marker element that Quill 2 adds inside each list item.</summary>
    public const string QuillUiClass = "ql-ui";

    /// <summary>The maximum length of the <c>alt</c> text of an image.</summary>
    public const int AltMaxLength = 255;

    /// <summary>
    /// The format of an encrypted file ID: the version prefix, then 24 ciphertext bytes in Base64Url (32 characters).
    /// </summary>
    public const string FileIdPattern = "^v[0-9]+\\.[A-Za-z0-9_-]{32}\\z";
}
