using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Shared.UnitTests.RichText.Data;

namespace HealthyFac.Shared.UnitTests.RichText;

public sealed class HtmlSanitizerRichTextPolicyTests
{
    private readonly HtmlSanitizerRichTextPolicy _policy = new();

    [Theory]
    [MemberData(nameof(RichTextTestData.ToolbarSamples), MemberType = typeof(RichTextTestData))]
    public void IsAllowed_WithToolbarSamples_ReturnsTrue(string html)
    {
        var isAllowed = _policy.IsAllowed(html);

        isAllowed.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(RichTextTestData.HostileHtml), MemberType = typeof(RichTextTestData))]
    public void IsAllowed_WithHostileHtml_ReturnsFalse(string html)
    {
        var isAllowed = _policy.IsAllowed(html);

        isAllowed.Should().BeFalse();
    }
}
