using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Shared.UnitTests.RichText.Data;

namespace HealthyFac.Shared.UnitTests.RichText;

public sealed class HtmlSanitizerRichTextPolicyTests
{
    private readonly HtmlSanitizerRichTextPolicy _policy = new();

    [Theory]
    [MemberData(nameof(RichTextTestData.ToolbarSamples), MemberType = typeof(RichTextTestData))]
    public void Inspect_WithToolbarSamples_IsAllowed(string html)
    {
        var inspection = _policy.Inspect(html);

        inspection.IsAllowed.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(RichTextTestData.HostileHtml), MemberType = typeof(RichTextTestData))]
    public void Inspect_WithHostileHtml_IsNotAllowed(string html)
    {
        var inspection = _policy.Inspect(html);

        inspection.IsAllowed.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(RichTextTestData.VisibleLengthCases), MemberType = typeof(RichTextTestData))]
    public void Inspect_WithAllowedHtml_CountsVisibleCharacters(string html, int expectedLength)
    {
        var inspection = _policy.Inspect(html);

        inspection.IsAllowed.Should().BeTrue();
        inspection.VisibleLength.Should().Be(expectedLength);
    }
}
