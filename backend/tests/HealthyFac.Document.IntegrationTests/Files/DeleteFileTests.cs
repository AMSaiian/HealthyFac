using HealthyFac.Document.Application.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class DeleteFileTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteCompanyFile_WithFile_SoftDeletesAndKeepsTheBlob()
    {
        var response = await _fixture.UploadCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
            cancellationToken: TestContext.Current.CancellationToken);

        await _fixture.DeleteCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = response.Id });

        var file = await _fixture.LoadFileRowAsync(response.Id);
        var blobs = await _fixture.LoadBlobNamesAsync();

        file.DeletedAt.Should().NotBeNull();
        file.DeletedBy.Should().NotBeNull();
        blobs.Should().Equal(response.Id.ToString());
    }

    [Fact]
    public async Task DeleteCompanyFile_WithFile_StoresOneFileDeletedMessage()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);

        await _fixture.DeleteCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = ids[index: 0] });

        var messages = await _fixture.ReadOutboxMessagesAsync<FileDeleted>();

        messages.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new FileDeleted { FileId = ids[index: 0], CompanyId = UpdateFileTestData.CompanyId });
    }

    [Fact]
    public async Task DeleteCompanyFile_WithFileOfAnotherCompany_StoresNoMessage()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.OtherCompanyId, [UpdateFileTestData.CompanyFile()]);
        var act = () => _fixture.DeleteCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = ids[index: 0] });
        await act.Should().ThrowAsync<NotFoundException>();

        var messages = await _fixture.ReadOutboxMessagesAsync<FileDeleted>();

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCompanyFile_Twice_Throws404ForTheSecondDelete()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);
        var request = new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = ids[index: 0] };
        await _fixture.DeleteCompanyFileAsync(UpdateFileTestData.CompanyId, request);

        var act = () => _fixture.DeleteCompanyFileAsync(UpdateFileTestData.CompanyId, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCompanyFile_WithFileOfAnotherCompany_Throws404()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.OtherCompanyId, [UpdateFileTestData.CompanyFile()]);

        var act = () => _fixture.DeleteCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = ids[index: 0] });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteMachineFile_WithMachineInScope_SoftDeletes()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);

        await _fixture.DeleteMachineFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest
            {
                CompanyId = UpdateFileTestData.CompanyId,
                MachineId = machineId,
                Id = ids[index: 0],
            });

        var file = await _fixture.LoadFileRowAsync(ids[index: 0]);

        file.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMachineFile_WithCompanyFile_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);

        var act = () => _fixture.DeleteMachineFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest
            {
                CompanyId = UpdateFileTestData.CompanyId,
                MachineId = machineId,
                Id = ids[index: 0],
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCompanyFile_WithMachineFile_SoftDeletes()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);

        await _fixture.DeleteCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest { CompanyId = UpdateFileTestData.CompanyId, Id = ids[index: 0] });

        var file = await _fixture.LoadFileRowAsync(ids[index: 0]);

        file.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMachineFile_WithFileOfAnotherMachine_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var otherMachineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt,
                    otherMachineId),
            ]);

        var act = () => _fixture.DeleteMachineFileAsync(
            UpdateFileTestData.CompanyId,
            new DeleteFileRequest
            {
                CompanyId = UpdateFileTestData.CompanyId,
                MachineId = machineId,
                Id = ids[index: 0],
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
