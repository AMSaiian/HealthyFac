using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class UpdateFileTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UpdateCompanyFile_WithStoredExtensionInName_StoresTheBaseName()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);

        await _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, rowVersion, fileName: "report.pdf"));

        var file = await _fixture.LoadFileRowAsync(id);

        file.FileName.Should().Be(expected: "report");
        file.Extension.Should().Be(expected: "pdf");
    }

    [Fact]
    public async Task UpdateCompanyFile_WithOtherExtensionInName_KeepsIt()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);

        await _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, rowVersion, fileName: "report.docx"));

        var file = await _fixture.LoadFileRowAsync(id);

        file.FileName.Should().Be(expected: "report.docx");
    }

    [Fact]
    public async Task UpdateCompanyFile_WithDescription_ChangesIt()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);

        await _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, rowVersion, description: UpdateFileTestData.Description));

        var file = await _fixture.LoadFileRowAsync(id);

        file.Description.Should().Be(UpdateFileTestData.Description);
        file.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCompanyFile_WithNameOfOnlyTheStoredExtension_Returns400()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, rowVersion, fileName: ".pdf"));

        await act.Should().ThrowAsync<ValidationException>();

        var file = await _fixture.LoadFileRowAsync(id);

        file.FileName.Should().Be(expected: "old");
    }

    [Fact]
    public async Task UpdateCompanyFile_WithStaleRowVersion_ThrowsConflict()
    {
        var id = await ArrangeCompanyFileAsync();
        var staleVersion = await _fixture.LoadRowVersionAsync(id);
        await _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, staleVersion, fileName: "first"));

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, staleVersion, fileName: "second"));

        await act.Should().ThrowAsync<ConflictException>();

        var file = await _fixture.LoadFileRowAsync(id);

        file.FileName.Should().Be(expected: "first");
    }

    [Fact]
    public async Task UpdateCompanyFile_InParallel_OneSucceedsAndOneConflicts()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);
        using var race = _fixture.SaveGate.StartRace(UpdateFileTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.UpdateCompanyFileAsync(
                UpdateFileTestData.CompanyId,
                UpdateFileTestData.CompanyUpdate(id, rowVersion, fileName: "first"))),
            race.RunAsync(() => _fixture.UpdateCompanyFileAsync(
                UpdateFileTestData.CompanyId,
                UpdateFileTestData.CompanyUpdate(id, rowVersion, fileName: "second"))));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.OfType<ConflictException>().Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateCompanyFile_WithDeletedFile_Throws404()
    {
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);
        await _fixture.DeleteFileRefAsync(UpdateFileTestData.CompanyId, id);

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(id, rowVersion));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCompanyFile_WithFileOfAnotherCompany_Throws404()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.OtherCompanyId, [UpdateFileTestData.CompanyFile()]);
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(ids[index: 0], rowVersion, companyId: UpdateFileTestData.CompanyId));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCompanyFile_WithMachineFile_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(ids[index: 0], rowVersion));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCompanyFile_WithEmbeddedImage_Throws404()
    {
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "image", Extension: "png", SizeBytes: 10, UpdateFileTestData.UploadedAt,
                    Purpose: FilePurpose.Embedded),
            ]);
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        var act = () => _fixture.UpdateCompanyFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.CompanyUpdate(ids[index: 0], rowVersion));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateMachineFile_WithMachineInScope_RenamesTheFile()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            UpdateFileTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "machine", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        await _fixture.UpdateMachineFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.MachineUpdate(machineId, ids[index: 0], rowVersion));

        var file = await _fixture.LoadFileRowAsync(ids[index: 0]);

        file.FileName.Should().Be(expected: "new");
    }

    [Fact]
    public async Task UpdateMachineFile_WithCompanyFile_Throws404()
    {
        var machineId = await _fixture.RegisterMachineAsync(UpdateFileTestData.CompanyId);
        var id = await ArrangeCompanyFileAsync();
        var rowVersion = await _fixture.LoadRowVersionAsync(id);

        var act = () => _fixture.UpdateMachineFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.MachineUpdate(machineId, id, rowVersion));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateMachineFile_WithFileOfAnotherMachine_Throws404()
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
        var rowVersion = await _fixture.LoadRowVersionAsync(ids[index: 0]);

        var act = () => _fixture.UpdateMachineFileAsync(
            UpdateFileTestData.CompanyId,
            UpdateFileTestData.MachineUpdate(machineId, ids[index: 0], rowVersion));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Guid> ArrangeCompanyFileAsync()
    {
        var ids = await _fixture.SeedFilesAsync(UpdateFileTestData.CompanyId, [UpdateFileTestData.CompanyFile()]);

        return ids[index: 0];
    }
}
