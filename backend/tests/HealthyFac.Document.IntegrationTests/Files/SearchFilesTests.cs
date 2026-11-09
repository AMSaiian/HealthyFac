using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class SearchFilesTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchCompanyFiles_Always_ReturnsOnlyActiveCompanyAttachments()
    {
        var machineId = await _fixture.RegisterMachineAsync(SearchFilesTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            SearchFilesTestData.CompanyId,
            [
                new FileSeed(FileName: "company", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt),
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt,
                    machineId),
                new FileSeed(
                    FileName: "embedded", Extension: "png", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt,
                    Purpose: FilePurpose.Embedded),
                new FileSeed(
                    FileName: "foreign",
                    Extension: "pdf",
                    SizeBytes: 10,
                    SearchFilesTestData.FirstUploadedAt,
                    CompanyId: SearchFilesTestData.OtherCompanyId),
                new FileSeed(FileName: "deleted", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt),
            ]);
        await _fixture.DeleteFileRefAsync(SearchFilesTestData.CompanyId, ids[index: 4]);

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0]);
        response.TotalCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task SearchMachineFiles_Always_ReturnsOnlyFilesOfThatMachine()
    {
        var machineId = await _fixture.RegisterMachineAsync(SearchFilesTestData.CompanyId);
        var otherMachineId = await _fixture.RegisterMachineAsync(SearchFilesTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            SearchFilesTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "mine", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt, machineId),
                new FileSeed(
                    FileName: "other", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt,
                    otherMachineId),
                new FileSeed(FileName: "company", Extension: "pdf", SizeBytes: 10, SearchFilesTestData.FirstUploadedAt),
            ]);

        var response = await _fixture.SearchMachineFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateMachineSearch(SearchFilesTestData.CompanyId, machineId));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 0]);
    }

    [Fact]
    public async Task SearchMachineFiles_WithMachineOfAnotherCompany_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(SearchFilesTestData.OtherCompanyId);

        var act = () => _fixture.SearchMachineFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateMachineSearch(SearchFilesTestData.CompanyId, machineId));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SearchCompanyFiles_WithNamePrefix_MatchesIgnoringCase()
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());
        var filter = new FileGridFilter { FileName = new TextFilter { Value = SearchFilesTestData.LowerAlphaPrefix } };

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId, filter));

        response.Items.Select(item => item.Id).Should().BeEquivalentTo([ids[index: 0], ids[index: 1]]);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithExtensionSet_ReturnsMatchingFiles()
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());
        var filter = new FileGridFilter { Extension = new SetFilter<string> { Values = ["png", "docx"] } };

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId, filter));

        response.Items.Select(item => item.Id).Should().BeEquivalentTo([ids[index: 1], ids[index: 3]]);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithUploadRange_IncludesFromAndExcludesTo()
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());
        var filter = new FileGridFilter
        {
            UploadedAt = new DateRangeFilter
            {
                From = SearchFilesTestData.FirstUploadedAt.AddDays(value: 1),
                To = SearchFilesTestData.FirstUploadedAt.AddDays(value: 3),
            },
        };

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId, filter));

        response.Items.Select(item => item.Id).Should().BeEquivalentTo([ids[index: 1], ids[index: 2]]);
    }

    [Theory]
    [MemberData(nameof(SearchFilesTestData.SortCases), MemberType = typeof(SearchFilesTestData))]
    public async Task SearchCompanyFiles_WithSort_OrdersTheRows(
        FileSortColumn column,
        SortDirection direction,
        int[] expectedOrder
    )
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());
        var sort = new GridSort<FileSortColumn> { Column = column, Direction = direction };

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId, sort: sort));

        response.Items.Select(item => item.Id).Should().Equal(expectedOrder.Select(index => ids[index]));
    }

    [Fact]
    public async Task SearchCompanyFiles_WithoutSort_ReturnsTheNewestUploadFirst()
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId));

        response.Items.Select(item => item.Id)
            .Should()
            .Equal(ids[index: 3], ids[index: 2], ids[index: 1], ids[index: 0]);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithPage_ReturnsThatPageAndTheTotal()
    {
        var ids = await _fixture.SeedFilesAsync(SearchFilesTestData.CompanyId, SearchFilesTestData.CreateSeeds());

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(
                SearchFilesTestData.CompanyId,
                offset: SearchFilesTestData.SecondPageOffset,
                limit: SearchFilesTestData.PageSize));

        response.Items.Select(item => item.Id).Should().Equal(ids[index: 1], ids[index: 0]);
        response.TotalCount.Should().Be(expected: 4);
    }

    [Fact]
    public async Task SearchCompanyFiles_Always_MapsTheGridRow()
    {
        var ids = await _fixture.SeedFilesAsync(
            SearchFilesTestData.CompanyId,
            [new FileSeed(FileName: "row", Extension: "pdf", SizeBytes: 123, SearchFilesTestData.FirstUploadedAt)]);

        var response = await _fixture.SearchCompanyFilesAsync(
            SearchFilesTestData.CompanyId,
            SearchFilesTestData.CreateCompanySearch(SearchFilesTestData.CompanyId));

        var item = response.Items.Single();

        item.Id.Should().Be(ids[index: 0]);
        item.FileName.Should().Be(expected: "row");
        item.Extension.Should().Be(expected: "pdf");
        item.ContentType.Should().Be(expected: "application/pdf");
        item.SizeBytes.Should().Be(expected: 123);
        item.UploadedAt.Should().Be(SearchFilesTestData.FirstUploadedAt);
        item.RowVersion.Should().BeGreaterThan(expected: 0u);
    }
}
