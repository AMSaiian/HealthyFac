using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class IngestMachineChangeRaceTests(InsightsFixture fixture)
{
    private static readonly Guid FirstMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    private static readonly Guid SecondMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000102");

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task FileMachineChanged_DuringTheRun_GivesTheStoredDocumentTheNewMachine()
    {
        _fixture.ArrangeFile(machineId: null);
        await _fixture.StartAttemptAsync();
        var changedAt = IngestAttemptTestData.Now.AddMinutes(value: 5);
        _fixture.Ingest.Converter.Behavior = async _ =>
        {
            await _fixture.HandleMachineChangedAsync(
                IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId, FirstMachineId, changedAt);

            return FakeDocumentConverter.DefaultDocument;
        };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        document!.MachineId.Should().Be(FirstMachineId);
        document.MachineChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public async Task FileMachineChanged_WithOlderTimeDuringTheRun_LeavesTheMachineOfTheFile()
    {
        _fixture.ArrangeFile(machineId: FirstMachineId);
        await _fixture.StartAttemptAsync();
        _fixture.Ingest.Converter.Behavior = async _ =>
        {
            await _fixture.HandleMachineChangedAsync(
                IngestAttemptTestData.CompanyId,
                IngestAttemptTestData.FileId,
                SecondMachineId,
                IngestAttemptTestData.Now.AddMinutes(value: -5));

            return FakeDocumentConverter.DefaultDocument;
        };

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        document!.MachineId.Should().Be(FirstMachineId);
        document.MachineChangedAt.Should().Be(IngestAttemptTestData.Now);
    }

    [Fact]
    public async Task FileMachineChanged_AfterTheFinalSave_UpdatesTheStoredDocument()
    {
        _fixture.ArrangeFile(machineId: null);
        await _fixture.StartAttemptAsync();
        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);
        var changedAt = IngestAttemptTestData.Now.AddMinutes(value: 5);

        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId, IngestAttemptTestData.FileId, SecondMachineId, changedAt);

        var document = await _fixture.LoadDocumentAsync(IngestAttemptTestData.FileId);

        document!.MachineId.Should().Be(SecondMachineId);
    }
}
