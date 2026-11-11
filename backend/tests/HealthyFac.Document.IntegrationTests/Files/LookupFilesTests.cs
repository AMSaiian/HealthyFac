using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Lookup;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
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
        await _fixture.DeleteFileRefAsync(GetFileLinksTestData.CompanyId, ids[index: 3]);
        var requested = ids.Select(_fixture.Encrypt)
            .Concat([GetFileLinksTestData.MalformedId, _fixture.Encrypt(Guid.CreateVersion7())])
            .Append(_fixture.Encrypt(ids[index: 0]));

        var response = await LookupAsync(GetFileLinksTestData.CompanyId, requested);

        response.Files.Select(file => file.Id).Should().BeEquivalentTo([ids[index: 0], ids[index: 2]]);
        var machineFile = response.Files.Single(file => file.Id == ids[index: 2]);
        machineFile.MachineId.Should().Be(machineId);
        machineFile.FileName.Should().Be(expected: "machine");
        machineFile.Extension.Should().Be(expected: "pdf");
        machineFile.SizeBytes.Should().Be(expected: 20);
        response.Files.Single(file => file.Id == ids[index: 0]).MachineId.Should().BeNull();
    }

    [Fact]
    public void Validate_WithIdCountAtAndAboveTheLimit_AcceptsOnlyTheLimit()
    {
        var validator = new LookupFilesRequestValidator();

        var atLimit = validator.Validate(
            new LookupFilesRequest
            {
                CompanyId = GetFileLinksTestData.CompanyId,
                Ids = [.. Enumerable.Range(start: 0, FileValidationConstants.LookupIdsMaxCount).Select(_ => "id")],
            });
        var above = validator.Validate(
            new LookupFilesRequest
            {
                CompanyId = GetFileLinksTestData.CompanyId,
                Ids = [.. Enumerable.Range(start: 0, FileValidationConstants.LookupIdsMaxCount + 1).Select(_ => "id")],
            });

        atLimit.IsValid.Should().BeTrue();
        above.IsValid.Should().BeFalse();
    }

    private async Task<LookupFilesResponse> LookupAsync(Guid companyId, IEnumerable<string> ids)
    {
        using var scope = _fixture.CreateUserScope(companyId);

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<LookupFilesRequest, LookupFilesResponse>>()
            .HandleAsync(new LookupFilesRequest { CompanyId = companyId, Ids = [.. ids] });
    }
}
