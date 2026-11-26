using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;

namespace HealthyFac.Insights.IntegrationTests.Documents;

[Collection(SignalCaseCollection.Name)]
public sealed class ChangeIngestedFileMachineInboxTests(SignalCaseFixture fixture)
{
    private static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task Receive_TheMachineChangeThroughTheInbox_ChangesTheDocumentAndMarksTheRowProcessed()
    {
        var completed = IngestAttemptTestData.CreateCompleted();
        await _fixture.Inner.SaveAsync(completed, IngestAttemptTestData.CreateDocument(completed.Id));
        var changedAt = IngestAttemptTestData.Now.AddMinutes(value: 5);

        var message = new FileMachineChanged
        {
            FileId = IngestAttemptTestData.FileId,
            CompanyId = IngestAttemptTestData.CompanyId,
            MachineId = MachineId,
            ChangedAt = changedAt,
        };

        var outcome = await _fixture.ReceiveThroughInboxAsync(message, IngestAttemptTestData.CompanyId);

        var document = await _fixture.Inner.LoadDocumentAsync(IngestAttemptTestData.FileId);
        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        document!.MachineId.Should().Be(MachineId);
    }
}
