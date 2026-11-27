using System.Text.Json;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Insights.UnitTests.Messaging.Data;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
[Feature(TestFeatures.Drafts)]
public sealed class ProposalSourceJsonTests
{
    [Fact]
    public void Deserialize_WithTheTextChat_ReadsTheChatSourceOfBothMessages()
    {
        var workOrderJson = JsonSerializer.Serialize(
            WorkOrderDraftProposedTestData.Chat(), MessagingJsonOptions.Instance);

        var proposed = JsonSerializer.Deserialize<DraftsProposed>(
            json: """{"source":"chat","companyId":"0190f3a0-0000-7000-8000-0000000000a1"}""",
            MessagingJsonOptions.Instance);

        var workOrder = JsonSerializer.Deserialize<WorkOrderDraftProposed>(
            workOrderJson,
            MessagingJsonOptions.Instance);

        proposed!.Source.Should().Be(ProposalSource.Chat);
        workOrder!.Source.Should().Be(WorkOrderProposalSource.Chat);
    }

    [Fact]
    public void Deserialize_WithAnUnknownSourceText_Fails()
    {
        var act = () => JsonSerializer.Deserialize<DraftsProposed>(
            json: """{"source":"other","companyId":"0190f3a0-0000-7000-8000-0000000000a1"}""",
            MessagingJsonOptions.Instance);

        act.Should().Throw<JsonException>();
    }
}
