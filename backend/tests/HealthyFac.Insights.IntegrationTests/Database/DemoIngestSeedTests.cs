using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Insights.IntegrationTests.Database;

[Collection(InsightsCollection.Name)]
public sealed class DemoIngestSeedTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task SeedAsync_WithATestTenant_CreatesTheCompletedAttemptsAsTheSupervisor()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var attempts = await _fixture.LoadSeededAttemptsAsync();
        var documents = await _fixture.LoadSeededDocumentsAsync();
        var chunks = await _fixture.LoadSeededChunksAsync();

        attempts.Should().HaveCount(DemoIngestSeedTestData.AttemptCount);
        attempts.Should().OnlyContain(attempt => attempt.State == IngestState.Completed);
        attempts.Should().OnlyContain(attempt => attempt.CompanyId == DemoIngestSeedFixtureExtensions.CompanyId);
        attempts.Should().OnlyContain(attempt => attempt.CreatedBy == DemoIngestSeedFixtureExtensions.SupervisorId);
        attempts.Should().OnlyContain(attempt => attempt.LeaseUntil == null);
        documents.Should().HaveCount(DemoIngestSeedTestData.AttemptCount);
        documents.Should().OnlyContain(document => document.CompanyId == DemoIngestSeedFixtureExtensions.CompanyId);
        chunks.Should().HaveCount(DemoIngestSeedTestData.ChunkCount);

        var cncAttempt = attempts.Single(attempt => attempt.FileId == DemoIngestSeedTestData.CncFileId);
        (cncAttempt.PageCount, cncAttempt.ChunkCount, cncAttempt.ProposedDraftCount).Should().Be((24, 62, 55));
        documents.Single(document => document.Id == DemoIngestSeedTestData.CncFileId)
            .ChunkCount.Should()
            .Be(
                chunks.Count(chunk => chunk.DocumentId == DemoIngestSeedTestData.CncFileId));
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_CreatesTheScreenerAndExtractorRunsWithTheirToolCalls()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var runs = await _fixture.LoadSeededRunsAsync();
        var calls = await _fixture.LoadSeededToolCallsAsync();

        runs.Should().HaveCount(DemoIngestSeedTestData.RunCount);
        runs.Should().OnlyContain(run => run.State == AgentRunState.Completed && run.IngestAttemptId != null);
        runs.Count(run => run.Role == AgentRole.Extractor).Should().Be(DemoIngestSeedTestData.AttemptCount);
        runs.Count(run => run.Role == AgentRole.Screener).Should().Be(DemoIngestSeedTestData.AttemptCount);
        calls.Should().HaveCount(DemoIngestSeedTestData.ToolCallCount);
        calls.Should().OnlyContain(call => call.Outcome == ToolCallOutcome.Succeeded);
        calls.Select(call => call.RunId).Distinct().Should().OnlyContain(id => runs.Any(run => run.Id == id));
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_StoresEveryVectorWithUnitNorm()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var chunks = await _fixture.LoadSeededChunksAsync();

        chunks.Should().OnlyContain(chunk => chunk.Embedding.Length == DemoIngestSeedTestData.EmbeddingDimensions);
        chunks.Should()
            .OnlyContain(chunk => Math.Abs(Math.Sqrt(chunk.Embedding.Sum(value => (double)value * value)) - 1)
                < DemoIngestSeedTestData.NormTolerance);
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_PlacesTheNewestFinishOneHourBeforeTheSeedTime()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var attempts = await _fixture.LoadSeededAttemptsAsync();

        attempts.Max(attempt => attempt.FinishedAt)
            .Should()
            .Be(
                (DemoIngestSeedFixtureExtensions.SeedTime - SeedTimeShift.NewestAge).UtcDateTime);
    }

    [Fact]
    public async Task SeedAsync_OnAnEmptyDatabase_AddsTheCaseAndTheSignal()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var cases = await _fixture.LoadSeededCasesAsync();

        var maintenanceCase = cases.Should().ContainSingle().Subject;
        maintenanceCase.Id.Should().Be(DemoIngestSeedTestData.CaseId);
        maintenanceCase.State.Should().Be(CaseState.Open);
        maintenanceCase.MachineId.Should().Be(DemoIngestSeedTestData.MachineId);
        maintenanceCase.CompanyId.Should().Be(DemoIngestSeedFixtureExtensions.CompanyId);
        var signal = maintenanceCase.Signals.Should().ContainSingle().Subject;
        signal.Id.Should().Be(DemoIngestSeedTestData.SignalId);
        signal.CaseId.Should().Be(DemoIngestSeedTestData.CaseId);
        signal.Coverage.Should().Be(SignalCoverage.Drafted);
        signal.WorkOrderDraftId.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedAsync_Twice_AddsTheCaseOnce()
    {
        await _fixture.RunDemoIngestSeedAsync();

        await _fixture.RunDemoIngestSeedAsync();

        var cases = await _fixture.LoadSeededCasesAsync();

        cases.Should().ContainSingle();
        cases.Single().Signals.Should().ContainSingle();
    }

    [Fact]
    public async Task SeedAsync_RunTwice_AddsNothing()
    {
        await _fixture.RunDemoIngestSeedAsync();

        await _fixture.RunDemoIngestSeedAsync();

        var attempts = await _fixture.LoadSeededAttemptsAsync();
        var chunks = await _fixture.LoadSeededChunksAsync();

        var runs = await _fixture.LoadSeededRunsAsync();

        attempts.Should().HaveCount(DemoIngestSeedTestData.AttemptCount);
        chunks.Should().HaveCount(DemoIngestSeedTestData.ChunkCount);
        runs.Should().HaveCount(DemoIngestSeedTestData.RunCount);
    }

    [Fact]
    public async Task SeedAsync_AfterTheDocumentOfADeletedFileWasRemoved_DoesNotCreateItAgain()
    {
        await _fixture.RunDemoIngestSeedAsync();
        await _fixture.RemoveSeededDocumentAsync(DemoIngestSeedTestData.CncFileId);

        await _fixture.RunDemoIngestSeedAsync();

        var documents = await _fixture.LoadSeededDocumentsAsync();
        var chunks = await _fixture.LoadSeededChunksAsync();

        documents.Should().NotContain(document => document.Id == DemoIngestSeedTestData.CncFileId);
        documents.Should().HaveCount(DemoIngestSeedTestData.AttemptCount - 1);
        chunks.Should().NotContain(chunk => chunk.DocumentId == DemoIngestSeedTestData.CncFileId);
        chunks.Should().HaveCount(DemoIngestSeedTestData.ChunkCount - DemoIngestSeedTestData.CncChunkCount);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_InTheDevelopmentEnvironment_CreatesTheDemoAttempts()
    {
        await _fixture.RunInitializerSeedAsync(Environments.Development);

        var attempts = await _fixture.LoadSeededAttemptsAsync();

        attempts.Should().HaveCount(DemoIngestSeedTestData.AttemptCount);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OutsideTheDevelopmentEnvironment_CreatesNoDemoAttempt()
    {
        await _fixture.RunInitializerSeedAsync(Environments.Production);

        var attempts = await _fixture.LoadSeededAttemptsAsync();

        attempts.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_AfterTheSeed_LetsTheWorkerClaimNothing()
    {
        await _fixture.RunDemoIngestSeedAsync();

        var claimed = await _fixture.ClaimAttemptAsync();

        claimed.Should().BeNull();
    }

    [Fact]
    public async Task SeedAsync_WithoutATestTenant_CreatesNothing()
    {
        await _fixture.RunDemoIngestSeedAsync(hasTenant: false);

        var attempts = await _fixture.LoadSeededAttemptsAsync();
        var chunks = await _fixture.LoadSeededChunksAsync();

        attempts.Should().BeEmpty();
        chunks.Should().BeEmpty();
    }
}
