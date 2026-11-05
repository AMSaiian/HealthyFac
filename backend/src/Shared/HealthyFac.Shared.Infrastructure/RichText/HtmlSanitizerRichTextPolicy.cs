using AngleSharp.Css.Dom;
using Ganss.Xss;
using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Shared.Infrastructure.RichText;

/// <summary>
/// Inspects rich text with HtmlSanitizer. The allow-list holds the formats of the editor toolbar (bold, italic,
/// underline, strikethrough, lists, links) and the markup that Quill 2 adds for lists. The policy never compares the
/// sanitized text with the original: it reports every removal that the sanitizer announces through its events, so a
/// difference in white space or entity encoding is never a finding.
/// </summary>
/// <remarks>
/// A sanitizer with event handlers keeps the state of one call, so each call builds its own sanitizer. One
/// <c>SanitizeDom</c> call gives the removal events and the parsed body, so one parse serves both results.
/// </remarks>
public sealed class HtmlSanitizerRichTextPolicy : IRichTextPolicy
{
    private static readonly string[] AllowedTags = ["p", "br", "strong", "em", "u", "s", "ol", "ul", "li", "a", "span"];

    private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

    private static readonly string[] AllowedAttributes =
    [
        RichTextAttributes.Href, RichTextAttributes.Target, RichTextAttributes.Rel, RichTextAttributes.Class,
        RichTextAttributes.DataList, RichTextAttributes.ContentEditable,
    ];

    public RichTextInspection Inspect(string html)
    {
        var isAllowed = true;
        var sanitizer = CreateSanitizer();

        void Reject(object? sender, EventArgs eventArgs)
        {
            isAllowed = false;
        }

        sanitizer.RemovingTag += Reject;
        sanitizer.RemovingAttribute += Reject;
        sanitizer.RemovingStyle += Reject;
        sanitizer.RemovingAtRule += Reject;
        sanitizer.RemovingComment += Reject;
        sanitizer.RemovingCssClass += Reject;

        var document = sanitizer.SanitizeDom(html);

        return new RichTextInspection
        {
            IsAllowed = isAllowed,
            VisibleLength = document.Body?.TextContent.Normalize().Length ?? 0,
        };
    }

    private static HtmlSanitizer CreateSanitizer() =>
        new(
            new HtmlSanitizerOptions
            {
                AllowedTags = new HashSet<string>(AllowedTags, StringComparer.OrdinalIgnoreCase),
                AllowedAttributes = new HashSet<string>(AllowedAttributes, StringComparer.OrdinalIgnoreCase),
                AllowedSchemes = new HashSet<string>(AllowedSchemes, StringComparer.OrdinalIgnoreCase),
                AllowedClasses = new HashSet<string>([RichTextAttributes.QuillUiClass]),
                AllowedCssProperties = new HashSet<string>(),
                AllowedAtRules = new HashSet<CssRuleType>(),
            });
}
