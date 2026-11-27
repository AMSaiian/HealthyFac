using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Insights.UnitTests.Messaging.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.WorkOrders)]
public sealed class WorkOrderDraftProposedValidatorTests
{
    private readonly WorkOrderDraftProposedValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(WorkOrderDraftProposedTestData.InvalidMessages),
        MemberType = typeof(WorkOrderDraftProposedTestData))]
    public void Validate_WithBrokenSourceRule_FailsWithThatRuleOnly(
        WorkOrderDraftProposed message,
        string expectedError
    )
    {
        var result = _validator.Validate(message);

        result.Errors.Select(error => error.ErrorMessage).Distinct().Should().Equal(expectedError);
    }

    [Theory]
    [MemberData(
        nameof(WorkOrderDraftProposedTestData.ValidMessages),
        MemberType = typeof(WorkOrderDraftProposedTestData))]
    public void Validate_WithTheFieldsOfItsSource_Passes(WorkOrderDraftProposed message)
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeTrue();
    }
}
