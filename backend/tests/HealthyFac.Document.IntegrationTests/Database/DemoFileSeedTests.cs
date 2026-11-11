using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Document.IntegrationTests.Database;

[Collection(DocumentSeedingCollection.Name)]
public sealed class DemoFileSeedTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedAsync_WithATestTenant_CreatesTheDemoFilesAsTheSupervisor()
    {
        await _fixture.RunDemoFileSeedAsync();

        var files = await _fixture.LoadDemoFilesAsync();

        files.Should().HaveCount(DemoFileSeedTestData.FileCount);
        files.Should().OnlyContain(file => file.CompanyId == DevelopmentSeedTestData.CompanyId);
        files.Should().OnlyContain(file => file.CreatedBy == DemoFileSeedTestData.SupervisorId);
        files.Should().OnlyContain(file => file.IngestState == FileIngestState.Completed);

        var cncFile = files.Single(file => file.Id == DemoFileSeedTestData.CncFileId);
        cncFile.MachineId.Should().Be(DemoFileSeedTestData.CncMachineId);
        files.Where(file => file.Id != DemoFileSeedTestData.CncFileId)
            .Should()
            .OnlyContain(file => file.MachineId == null);
    }

    [Fact]
    public async Task SeedAsync_WithoutATestTenant_CreatesNothing()
    {
        await _fixture.RunDemoFileSeedAsync(hasTenant: false);

        var files = await _fixture.LoadDemoFilesAsync(isDeletedIncluded: true);

        files.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_WritesTheBytesOfEachFileUnderItsId()
    {
        await _fixture.RunDemoFileSeedAsync();

        var files = await _fixture.LoadDemoFilesAsync();
        var hashes = new Dictionary<Guid, string>();
        foreach (var file in files)
        {
            hashes[file.Id] = await _fixture.HashBlobAsync(file.Id);
        }

        files.Should().OnlyContain(file => hashes[file.Id] == Convert.ToHexStringLower(file.Sha256));
    }

    [Fact]
    public async Task SeedAsync_RunTwice_AddsNoRowAndWritesNoBlob()
    {
        await _fixture.RunDemoFileSeedAsync();
        var writtenAt = await _fixture.LoadBlobWrittenAtAsync(DemoFileSeedTestData.CncFileId);

        await _fixture.RunDemoFileSeedAsync();

        var files = await _fixture.LoadDemoFilesAsync(isDeletedIncluded: true);
        var writtenAgainAt = await _fixture.LoadBlobWrittenAtAsync(DemoFileSeedTestData.CncFileId);

        files.Should().HaveCount(DemoFileSeedTestData.FileCount);
        writtenAgainAt.Should().Be(writtenAt);
    }

    [Fact]
    public async Task SeedAsync_WithADeletedDemoFile_DoesNotCreateItAgain()
    {
        await _fixture.RunDemoFileSeedAsync();
        await _fixture.DeleteFileRefAsync(DemoFileSeedTestData.CncFileId);

        await _fixture.RunDemoFileSeedAsync();

        var active = await _fixture.LoadDemoFilesAsync();
        var all = await _fixture.LoadDemoFilesAsync(isDeletedIncluded: true);

        active.Should().HaveCount(DemoFileSeedTestData.FileCount - 1);
        all.Should().HaveCount(DemoFileSeedTestData.FileCount);
    }

    [Fact]
    public async Task SeedAsync_WithARowAndNoBlob_WritesTheMissingBlob()
    {
        await _fixture.RunDemoFileSeedAsync();
        await _fixture.DeleteBlobAsync(DemoFileSeedTestData.CncFileId);

        await _fixture.RunDemoFileSeedAsync();

        var hash = await _fixture.HashBlobAsync(DemoFileSeedTestData.CncFileId);
        var files = await _fixture.LoadDemoFilesAsync();

        var cncFile = files.Single(file => file.Id == DemoFileSeedTestData.CncFileId);
        hash.Should().Be(Convert.ToHexStringLower(cncFile.Sha256));
    }

    [Fact]
    public async Task SeedAsync_WithATestTenant_ShiftsTheTimesAndKeepsTheirGap()
    {
        await _fixture.RunDemoFileSeedAsync();

        var files = await _fixture.LoadDemoFilesAsync();

        var cncFile = files.Single(file => file.Id == DemoFileSeedTestData.CncFileId);
        var offset = DemoFileSeedTestData.SeedTime
            - SeedTimeShift.NewestAge
            - DemoFileSeedTestData.ReferenceTimeInSnapshot;
        cncFile.IngestStateChangedAt.Should()
            .Be(DemoFileSeedTestData.CncIngestStateChangedAtInSnapshot + offset);
        (cncFile.IngestStateChangedAt - cncFile.MachineChangedAt).Should()
            .Be(
                DemoFileSeedTestData.CncIngestStateChangedAtInSnapshot
                - DemoFileSeedTestData.CncMachineChangedAtInSnapshot);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_InTheDevelopmentEnvironment_CreatesTheDemoFiles()
    {
        await _fixture.RunInitializerSeedAsync(Environments.Development);

        var files = await _fixture.LoadDemoFilesAsync();

        files.Should().HaveCount(DemoFileSeedTestData.FileCount);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_OutsideTheDevelopmentEnvironment_CreatesNoDemoFile()
    {
        await _fixture.RunInitializerSeedAsync(Environments.Production);

        var files = await _fixture.LoadDemoFilesAsync(isDeletedIncluded: true);

        files.Should().BeEmpty();
    }
}
