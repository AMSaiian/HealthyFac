using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.WorkOrders;

[Feature(TestFeatures.WorkOrders)]
[Feature(TestFeatures.Drafts)]
public sealed class WorkOrderDraftMessageLimitsTests
{
    [Fact]
    public void Limits_OfTheMessage_EqualTheWorkOrderConstants()
    {
        WorkOrderDraftMessageLimits.TitleMaxLength.Should().Be(WorkOrderValidationConstants.TitleMaxLength);
        WorkOrderDraftMessageLimits.DescriptionMaxLength.Should().Be(WorkOrderValidationConstants.DescriptionMaxLength);
        WorkOrderDraftMessageLimits.EstimateMinMinutes.Should().Be(WorkOrderValidationConstants.EstimateMinMinutes);
        WorkOrderDraftMessageLimits.EstimateMaxMinutes.Should().Be(WorkOrderValidationConstants.EstimateMaxMinutes);
        WorkOrderDraftMessageLimits.EstimateStepMinutes.Should().Be(WorkOrderValidationConstants.EstimateStepMinutes);
    }

    [Fact]
    public void ValueLists_OfTheMessage_EqualTheWorkOrderEnumerations()
    {
        WorkOrderDraftMessageLimits.Priorities.All.Should()
            .BeEquivalentTo(WorkOrderPriority.All.Select(priority => priority.Value));

        WorkOrderDraftMessageLimits.Categories.All.Should()
            .BeEquivalentTo(WorkOrderCategory.All.Select(category => category.Value));
    }
}
