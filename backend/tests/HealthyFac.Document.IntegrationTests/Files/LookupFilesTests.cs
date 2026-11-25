using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Lookup;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
[Feature(TestFeatures.Files)]
public sealed class LookupFilesTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task LookupFiles_WithFilesInScope_ReturnsTheirDataAndLeavesOutTheRest()
    {
        var machineId = await _fixture.RegisterMachineAsync(GetFileLinksTestData.CompanyId);
        var uploadedAt = GetFileLinksTestData.UploadedAt;

        var ids = await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [
                new FileSeed(FileName: "company", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(
                    FileName: "embedded", Extension: "png", SizeBytes: 10, uploadedAt, Purpose: FilePurpose.Embedded),
                new FileSeed(FileName: "machine", Extension: "pdf", SizeBytes: 20, uploadedAt, machineId),
                new FileSeed(FileName: "deleted", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(
                    FileName: "foreign", Extension: "pdf", SizeBytes: 10, uploadedAt,
                    CompanyId: GetFileLinksTestData.OtherCompanyId),
            ]);

        await _fixture.DeleteFileRefAsync(GetFileLinksTestData.CompanyId, ids[3]);

        var requested = ids.Select(_fixture.Encrypt)
            .Concat([GetFileLinksTestData.MalformedId, _fixture.Encrypt(Guid.CreateVersion7())])
            .Append(_fixture.Encrypt(ids[0]));

        var response = await LookupAsync(GetFileLinksTestData.CompanyId, requested);

        response.Files.Select(file => file.Id).Should().BeEquivalentTo([ids[0], ids[2]]);
        var machineFile = response.Files.Single(file => file.Id == ids[2]);
        machineFile.MachineId.Should().Be(machineId);
        machineFile.FileName.Should().Be(expected: "machine");
        machineFile.Extension.Should().Be(expected: "pdf");
        machineFile.SizeBytes.Should().Be(expected: 20);
        response.Files.Single(file => file.Id == ids[0]).MachineId.Should().BeNull();
    }

    [Fact]
    public async Task LookupFiles_WithASearch_ReturnsTheAttachmentsOfTheCompanyThatStartWithTheTextSortedByName()
    {
        var uploadedAt = GetFileLinksTestData.UploadedAt;
        var ownMachineId = await _fixture.RegisterMachineAsync(GetFileLinksTestData.CompanyId);
        var foreignMachineId = await _fixture.RegisterMachineAsync(GetFileLinksTestData.OtherCompanyId);

        var ids = await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [
                new FileSeed(FileName: "Manual c", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(FileName: "manual a", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(FileName: "manual b", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(FileName: "a manual", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(
                    FileName: "manual embedded", Extension: "png", SizeBytes: 10, uploadedAt,
                    Purpose: FilePurpose.Embedded),
                new FileSeed(
                    FileName: "manual foreign", Extension: "pdf", SizeBytes: 10, uploadedAt,
                    CompanyId: GetFileLinksTestData.OtherCompanyId),
                new FileSeed(FileName: "manual d machine", Extension: "pdf", SizeBytes: 10, uploadedAt, ownMachineId),
                new FileSeed(
                    FileName: "manual e foreign machine", Extension: "pdf", SizeBytes: 10, uploadedAt,
                    foreignMachineId),
            ]);

        var response = await LookupBySearchAsync(GetFileLinksTestData.CompanyId, search: "MANUAL");

        response.Files.Select(file => file.Id)
            .Should()
            .Equal(ids[1], ids[2], ids[0], ids[6]);
    }

    [Fact]
    public async Task LookupFiles_WithASearchThatMatchesMoreFilesThanTheCap_ReturnsTheCap()
    {
        await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [
                .. Enumerable.Range(start: 0, FileValidationConstants.LookupSearchMaxItems + 1)
                    .Select(number => new FileSeed(
                        $"cap-{number:D3}",
                        Extension: "pdf",
                        SizeBytes: 10,
                        GetFileLinksTestData.UploadedAt)),
            ]);

        var response = await LookupBySearchAsync(GetFileLinksTestData.CompanyId, search: "cap-");

        response.Files.Should().HaveCount(FileValidationConstants.LookupSearchMaxItems);
        response.Files.Select(file => file.FileName).Should().BeInAscendingOrder();
    }

    [Theory]
    [MemberData(nameof(LookupFilesTestData.IdsAndSearchCombinations), MemberType = typeof(LookupFilesTestData))]
    public void Validate_WithIdsAndSearch_AcceptsExactlyOneOfThem(
        string caseName,
        IReadOnlyList<string>? ids,
        string? search,
        bool isValid
    )
    {
        var validator = new LookupFilesRequestValidator();

        var result = validator.Validate(
            new LookupFilesRequest { CompanyId = GetFileLinksTestData.CompanyId, Ids = ids, Search = search });

        result.IsValid.Should().Be(isValid, caseName);
    }

    private async Task<LookupFilesResponse> LookupBySearchAsync(Guid companyId, string search)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupFilesRequest, LookupFilesResponse>>()
            .HandleAsync(new LookupFilesRequest { CompanyId = companyId, Search = search });
    }

    private async Task<LookupFilesResponse> LookupAsync(Guid companyId, IEnumerable<string> ids)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupFilesRequest, LookupFilesResponse>>()
            .HandleAsync(new LookupFilesRequest { CompanyId = companyId, Ids = [.. ids] });
    }
}
