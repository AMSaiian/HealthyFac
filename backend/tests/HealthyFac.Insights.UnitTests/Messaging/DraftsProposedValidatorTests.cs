using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Messaging.Data;
using HealthyFac.Shared.Infrastructure.RichText;

namespace HealthyFac.Insights.UnitTests.Messaging;

public sealed class DraftsProposedValidatorTests
{
    private readonly DraftsProposedValidator _validator = new(new HtmlSanitizerRichTextPolicy());

    [Theory]
    [MemberData(nameof(DraftsProposedTestData.InvalidMessages), MemberType = typeof(DraftsProposedTestData))]
    public void Validate_WithBrokenOwnRule_FailsWithThatRuleOnly(DraftsProposed message, string expectedError)
    {
        var result = _validator.Validate(message);

        result.Errors.Select(error => error.ErrorMessage).Distinct().Should().Equal(expectedError);
    }

    [Theory]
    [MemberData(nameof(DraftsProposedTestData.ValidMessages), MemberType = typeof(DraftsProposedTestData))]
    public void Validate_WithBoundaryValues_Passes(DraftsProposed message)
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeTrue();
    }
}
