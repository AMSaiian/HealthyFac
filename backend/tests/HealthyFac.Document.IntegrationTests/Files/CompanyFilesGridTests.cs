using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class CompanyFilesGridTests(DocumentDatabaseFixture fixture)
{
    private static readonly Guid CompanyId = CompanyFilesGridTestData.CompanyId;

    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchCompanyFiles_WithTiedAndUntiedFiles_ShowsMachineNameAndId()
    {
        var machineId = await RegisterMachineAsync(CompanyFilesGridTestData.PumpName);
        var ids = await _fixture.SeedFilesAsync(
            CompanyId, CompanyFilesGridTestData.CreateSeeds(machineId));

        var response = await SearchAsync();

        var tied = response.Items.Single(item => item.Id == ids[index: 0]);
        var untied = response.Items.Single(item => item.Id == ids[index: 1]);

        tied.MachineId.Should().Be(machineId);
        tied.MachineName.Should().Be(CompanyFilesGridTestData.PumpName);
        untied.MachineId.Should().BeNull();
        untied.MachineName.Should().BeNull();
    }

    [Fact]
    public async Task SearchCompanyFiles_WithUntiedFilter_ReturnsOnlyUntiedFiles()
    {
        var machineId = await RegisterMachineAsync(CompanyFilesGridTestData.PumpName);
        var ids = await _fixture.SeedFilesAsync(
            CompanyId, CompanyFilesGridTestData.CreateSeeds(machineId));

        var response = await SearchAsync(
            new FileGridFilter { Machine = new FileMachineFilter { MachineIds = [], IsUntiedIncluded = true } });

        response.Items.Select(item => item.Id).Should().BeEquivalentTo(ids.Skip(count: 1));
    }

    [Fact]
    public async Task SearchCompanyFiles_WithMachineFilter_ReturnsOnlyFilesOfThatMachine()
    {
        var machineId = await RegisterMachineAsync(CompanyFilesGridTestData.PumpName);
        var ids = await _fixture.SeedFilesAsync(
            CompanyId, CompanyFilesGridTestData.CreateSeeds(machineId));

        var response = await SearchAsync(
            new FileGridFilter { Machine = new FileMachineFilter { MachineIds = [machineId] } });

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0]);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithNeverFilter_ReturnsOnlyFilesWithoutIngest()
    {
        var ids = await _fixture.SeedFilesAsync(CompanyId, CompanyFilesGridTestData.CreateSeeds(tiedMachineId: null));
        await ApplyStateAsync(ids[index: 0], IngestMessageStates.Failed, failedCount: 1);

        var response = await SearchAsync(
            new FileGridFilter { IngestState = new FileIngestStateFilter { States = [], IsNeverIncluded = true } });

        response.Items.Select(item => item.Id).Should().BeEquivalentTo(ids.Skip(count: 1));
    }

    [Fact]
    public async Task SearchCompanyFiles_WithFailedFilter_ReturnsOnlyFailedFiles()
    {
        var ids = await _fixture.SeedFilesAsync(CompanyId, CompanyFilesGridTestData.CreateSeeds(tiedMachineId: null));
        await ApplyStateAsync(ids[index: 0], IngestMessageStates.Failed, failedCount: 1);
        await ApplyStateAsync(ids[index: 1], IngestMessageStates.Completed, failedCount: 0);

        var response = await SearchAsync(
            new FileGridFilter
            {
                IngestState = new FileIngestStateFilter { States = [IngestMessageStates.Failed] },
            });

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0]);
    }

    [Fact]
    public async Task SearchCompanyFiles_SortedByMachineName_OrdersByTheName()
    {
        var pumpId = await RegisterMachineAsync(CompanyFilesGridTestData.PumpName);
        var lathe = await RegisterMachineAsync(CompanyFilesGridTestData.LatheName);
        var ids = await _fixture.SeedFilesAsync(
            CompanyId,
            [
                new FileSeed(
                    FileName: "one", Extension: "pdf", SizeBytes: 10, CompanyFilesGridTestData.UploadedAt, pumpId),
                new FileSeed(
                    FileName: "two", Extension: "pdf", SizeBytes: 10, CompanyFilesGridTestData.UploadedAt, lathe),
            ]);

        var response = await _fixture.SearchCompanyFilesAsync(
            CompanyId,
            SearchFilesTestData.CreateCompanySearch(
                CompanyId,
                sort: new GridSort<FileSortColumn>
                {
                    Column = FileSortColumn.MachineName,
                    Direction = SortDirection.Ascending,
                }));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 1], ids[index: 0]);
    }

    [Theory]
    [MemberData(nameof(CompanyFilesGridTestData.CanIngestCases), MemberType = typeof(CompanyFilesGridTestData))]
    public async Task SearchCompanyFiles_WithIngestHistory_ComputesCanIngest(
        string extension,
        string? state,
        int failedCount,
        bool expected
    )
    {
        var ids = await _fixture.SeedFilesAsync(
            CompanyId,
            [new FileSeed(FileName: "file", extension, SizeBytes: 10, CompanyFilesGridTestData.UploadedAt)]);
        if (state is not null)
        {
            await ApplyStateAsync(ids[index: 0], state, failedCount);
        }

        var response = await SearchAsync();

        response.Items.Single().CanIngest.Should().Be(expected);
    }

    private async Task<Guid> RegisterMachineAsync(string name)
    {
        var message = MachineRegistrationTestData.CreateMessage() with { CompanyId = CompanyId, Name = name };
        await _fixture.RegisterMachineAsync(message);

        return message.MachineId;
    }

    private Task ApplyStateAsync(Guid fileId, string state, int failedCount) =>
        _fixture.ApplyIngestStateAsync(
            new IngestStateChanged
            {
                FileId = fileId,
                CompanyId = CompanyId,
                State = state,
                FailedCount = failedCount,
                ChangedAt = CompanyFilesGridTestData.UploadedAt.AddDays(value: 1),
            });

    private Task<GridResponse<FileGridItem>> SearchAsync(FileGridFilter? filter = null) =>
        _fixture.SearchCompanyFilesAsync(CompanyId, SearchFilesTestData.CreateCompanySearch(CompanyId, filter));
}
