using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Cases.Data;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[Collection(SignalCaseCollection.Name)]
public sealed class WorkOrderDraftResultTests(SignalCaseFixture fixture)
{
    private static readonly Guid DraftId = Guid.Parse(input: "66666666-6666-6666-6666-666666666666");
    private static readonly Guid WorkOrderId = Guid.Parse(input: "77777777-7777-7777-7777-777777777777");

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Receive_TheAppliedMessageThroughTheInbox_LinksTheDraftedSignals()
    {
        var caseId = await ArrangeDraftedSignalAsync();

        var outcome = await _fixture.ReceiveThroughInboxAsync(Applied(caseId), CaseTestData.CompanyId);

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        signal.Coverage.Should().Be(SignalCoverage.Linked);
        signal.WorkOrderId.Should().Be(WorkOrderId);
    }

    [Fact]
    public async Task Receive_TheDismissedMessageThroughTheInbox_DismissesTheDraftedSignalsAndQueuesNoRun()
    {
        var caseId = await ArrangeDraftedSignalAsync();

        var outcome = await _fixture.ReceiveThroughInboxAsync(Dismissed(caseId), CaseTestData.CompanyId);

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        var runs = await _fixture.LoadRunsAsync();
        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        signal.Coverage.Should().Be(SignalCoverage.Dismissed);
        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TheAppliedMessageForAnotherDraft_ChangesNothing()
    {
        var caseId = await ArrangeDraftedSignalAsync();
        var message = Applied(caseId) with { DraftId = Guid.CreateVersion7() };

        await _fixture.HandleWorkOrderDraftAppliedAsync(message);

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        signal.Coverage.Should().Be(SignalCoverage.Drafted);
    }

    [Fact]
    public async Task Handle_TheAppliedMessageTwice_LinksOnceAndChangesNothingTheSecondTime()
    {
        var caseId = await ArrangeDraftedSignalAsync();
        await _fixture.HandleWorkOrderDraftAppliedAsync(Applied(caseId));

        await _fixture.HandleWorkOrderDraftAppliedAsync(Applied(caseId) with { WorkOrderId = Guid.CreateVersion7() });

        var signal = await _fixture.LoadSignalAsync(CaseTestData.FirstEpisodeId);
        signal.WorkOrderId.Should().Be(WorkOrderId);
    }

    private static WorkOrderDraftApplied Applied(Guid caseId) =>
        new()
        {
            DraftId = DraftId,
            WorkOrderId = WorkOrderId,
            CaseId = caseId,
            CompanyId = CaseTestData.CompanyId,
            AppliedAt = CaseTestData.Now,
        };

    private static WorkOrderDraftDismissed Dismissed(Guid caseId) =>
        new()
        {
            DraftId = DraftId,
            CaseId = caseId,
            CompanyId = CaseTestData.CompanyId,
            DismissedAt = CaseTestData.Now,
        };

    private async Task<Guid> ArrangeDraftedSignalAsync()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
        await _fixture.ClearRunsAsync();
        await _fixture.DraftSignalWithDraftIdAsync(CaseTestData.FirstEpisodeId, DraftId);

        return (await _fixture.LoadCasesAsync()).Single().Id;
    }
}
