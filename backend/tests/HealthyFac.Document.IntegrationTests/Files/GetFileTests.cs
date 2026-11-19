using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class GetFileTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetCompanyFile_WithCompanyAttachment_ReturnsTheProjectedRow()
    {
        var response = await _fixture.UploadCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
            UploadFileTestData.Description,
            TestContext.Current.CancellationToken);
        var rowVersion = await _fixture.LoadRowVersionAsync(response.Id);

        var file = await _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, response.Id);

        file.Id.Should().Be(response.Id);
        file.FileName.Should().Be(UploadFileTestData.StoredPdfName);
        file.Extension.Should().Be(expected: "pdf");
        file.Description.Should().Be(UploadFileTestData.Description);
        file.SizeBytes.Should().Be(UploadFileTestData.Pdf.Length);
        file.MachineId.Should().BeNull();
        file.RowVersion.Should().Be(rowVersion);
    }

    [Fact]
    public async Task GetCompanyFile_WithMachineAttachment_ReturnsIt()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);

        var file = await _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, ids[index: 0]);

        file.Id.Should().Be(ids[index: 0]);
        file.MachineId.Should().Be(machineId);
    }

    [Fact]
    public async Task GetMachineFile_WithAttachmentOfTheMachine_ReturnsIt()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);

        var file = await _fixture.GetMachineFileAsync(UpdateFileTestData.CompanyId, machineId, ids[index: 0]);

        file.Id.Should().Be(ids[index: 0]);
        file.FileName.Should().Be(expected: "machine");
        file.MachineId.Should().Be(machineId);
    }

    [Fact]
    public async Task GetCompanyFile_AfterAnUpdate_ReturnsTheRowVersionThatTheNextUpdateNeeds()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);
        await _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(ids[index: 0], rowVersion, fileName: "first"));
        var storedRowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        var file = await _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, ids[index: 0]);

        file.RowVersion.Should().Be(storedRowVersion);
        file.RowVersion.Should().NotBe(rowVersion);
    }

    [Fact]
    public async Task GetCompanyFile_WithUnknownId_Throws404()
    {
        var act = () => _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, Guid.CreateVersion7());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCompanyFile_WithFileOfAnotherCompany_Throws404()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.OtherCompanyId, [UpdateFileTestData.CompanyFile()]);

        var act = () => _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCompanyFile_WithEmbeddedImage_Throws404()
    {
        var response = await _fixture.UploadEmbeddedImageAsync(
            UpdateFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PngName, UploadFileTestData.Png, UploadFileTestData.PngType));

        var act = () => _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, response.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCompanyFile_WithDeletedFile_Throws404()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);
        await _fixture.DeleteFileRefAsync(UpdateFileTestData.CompanyId, ids[index: 0]);

        var act = () => _fixture.GetCompanyFileAsync(UpdateFileTestData.CompanyId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMachineFile_WithFileOfAnotherMachine_Throws404()
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

        var act = () => _fixture.GetMachineFileAsync(UpdateFileTestData.CompanyId, machineId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMachineFile_WithCompanyFileWithoutMachine_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);

        var act = () => _fixture.GetMachineFileAsync(UpdateFileTestData.CompanyId, machineId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMachineFile_WithMachineOfAnotherCompany_Throws404()
    {
        var otherMachineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.OtherCompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);

        var act = () => _fixture.GetMachineFileAsync(UpdateFileTestData.CompanyId, otherMachineId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMachineFile_WithDeletedMachine_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);
        await _fixture.UnregisterMachineAsync(
            new MachineDeleted
            {
                MachineId = machineId,
                CompanyId = UpdateFileTestData.CompanyId,
            });

        var act = () => _fixture.GetMachineFileAsync(UpdateFileTestData.CompanyId, machineId, ids[index: 0]);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
