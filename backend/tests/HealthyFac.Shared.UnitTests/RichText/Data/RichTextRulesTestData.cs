using FluentValidation;
using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Shared.UnitTests.RichText.Data;

public sealed record RichTextRequest
{
    public string? Body { get; init; }
}

public sealed class RecordingRichTextPolicy(bool isAllowed, int visibleLength) : IRichTextPolicy
{
    private readonly bool _isAllowed = isAllowed;
    private readonly int _visibleLength = visibleLength;

    public int CallCount { get; private set; }

    public RichTextInspection Inspect(string html)
    {
        CallCount++;

        return new RichTextInspection { IsAllowed = _isAllowed, VisibleLength = _visibleLength };
    }
}

public sealed class RichTextRequestValidator : AbstractValidator<RichTextRequest>
{
    public RichTextRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.Body)
            .IsRichText(
                richTextPolicy,
                RichTextRulesTestData.MaxVisibleLength,
                RichTextRulesTestData.TooLongMessage,
                RichTextRulesTestData.NotAllowedMessage,
                RichTextRulesTestData.TooMuchFormattingMessage
            );
    }
}

public static class RichTextRulesTestData
{
    public const int MaxVisibleLength = 5000;

    public const string TooLongMessage = "Body is too long.";

    public const string NotAllowedMessage = "Body is not allowed.";

    public const string TooMuchFormattingMessage = "Body formatting is too large.";

    public const string SmallHtml = "<p>x</p>";

    public static string HtmlAboveCap => new(c: 'a', RichTextConstants.HtmlMaxLength + 1);

    public static string HtmlAtCap => new(c: 'a', RichTextConstants.HtmlMaxLength);
}
