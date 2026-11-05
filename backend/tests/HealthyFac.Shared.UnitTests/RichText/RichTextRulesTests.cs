using HealthyFac.Shared.UnitTests.RichText.Data;

namespace HealthyFac.Shared.UnitTests.RichText;

public sealed class RichTextRulesTests
{
    [Fact]
    public void IsRichText_WithNull_PassesWithoutCallingPolicy()
    {
        var policy = new RecordingRichTextPolicy(false, int.MaxValue);

        var result = new RichTextRequestValidator(policy).Validate(new RichTextRequest { Body = null });

        result.IsValid.Should().BeTrue();
        policy.CallCount.Should().Be(0);
    }

    [Fact]
    public void IsRichText_WithHtmlAboveCap_ReportsOnlyFormattingAndSkipsPolicy()
    {
        var policy = new RecordingRichTextPolicy(false, int.MaxValue);

        var result = new RichTextRequestValidator(policy)
            .Validate(new RichTextRequest { Body = RichTextRulesTestData.HtmlAboveCap });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(RichTextRulesTestData.TooMuchFormattingMessage);
        policy.CallCount.Should().Be(0);
    }

    [Fact]
    public void IsRichText_WithHtmlAtCapAndVisibleTextWithinLimit_IsValid()
    {
        var policy = new RecordingRichTextPolicy(true, RichTextRulesTestData.MaxVisibleLength);

        var result = new RichTextRequestValidator(policy)
            .Validate(new RichTextRequest { Body = RichTextRulesTestData.HtmlAtCap });

        result.IsValid.Should().BeTrue();
        policy.CallCount.Should().Be(1);
    }

    [Fact]
    public void IsRichText_WithRejectedHtml_ReportsOnlyNotAllowedEvenWhenVisibleTextIsTooLong()
    {
        var policy = new RecordingRichTextPolicy(false, RichTextRulesTestData.MaxVisibleLength + 1);

        var result = new RichTextRequestValidator(policy)
            .Validate(new RichTextRequest { Body = RichTextRulesTestData.SmallHtml });

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(RichTextRulesTestData.NotAllowedMessage);
        policy.CallCount.Should().Be(1);
    }

    [Fact]
    public void IsRichText_WithVisibleTextAboveLimit_ReportsTooLong()
    {
        var policy = new RecordingRichTextPolicy(true, RichTextRulesTestData.MaxVisibleLength + 1);

        var result = new RichTextRequestValidator(policy)
            .Validate(new RichTextRequest { Body = RichTextRulesTestData.SmallHtml });

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(RichTextRulesTestData.TooLongMessage);
    }

    [Fact]
    public void IsRichText_WithVisibleTextAtLimit_IsValid()
    {
        var policy = new RecordingRichTextPolicy(true, RichTextRulesTestData.MaxVisibleLength);

        var result = new RichTextRequestValidator(policy)
            .Validate(new RichTextRequest { Body = RichTextRulesTestData.SmallHtml });

        result.IsValid.Should().BeTrue();
    }
}
