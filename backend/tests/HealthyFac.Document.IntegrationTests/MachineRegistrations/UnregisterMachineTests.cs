using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.MachineRegistrations;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class UnregisterMachineTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UnregisterMachine_WithRegistration_SoftDeletes()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);

        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(message.MachineId));

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.DeletedAt.Should().NotBeNull();
        registration.DeletedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithFiles_SoftDeletesThemInOneSave()
    {
        var machineId = await _fixture.RegisterMachineAsync(MachineRegistrationTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            MachineRegistrationTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "one", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
                new FileSeed(
                    FileName: "two", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);
        using var race = _fixture.SaveGate.StartRace(participantCount: 1);

        var failure = await race.RunAsync(() => _fixture.UnregisterMachineAsync(
            MachineRegistrationTestData.CreateDeletedMessage(machineId)));

        var first = await _fixture.LoadFileRowAsync(ids[index: 0]);
        var second = await _fixture.LoadFileRowAsync(ids[index: 1]);

        failure.Should().BeNull();
        race.SaveCounts.Should().Equal(1);
        first.DeletedAt.Should().NotBeNull();
        second.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithFiles_StoresOneFileDeletedMessageForEachFile()
    {
        var machineId = await _fixture.RegisterMachineAsync(MachineRegistrationTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            MachineRegistrationTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "one", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
                new FileSeed(
                    FileName: "two", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, machineId),
            ]);

        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(machineId));

        var messages = await _fixture.ReadOutboxMessagesAsync<FileDeleted>();

        messages.Select(message => message.FileId).Should().BeEquivalentTo(ids);
        messages.Should().OnlyContain(message => message.CompanyId == MachineRegistrationTestData.CompanyId);
    }

    [Fact]
    public async Task UnregisterMachine_WithoutRegistration_StoresNoMessage()
    {
        var act = () => _fixture.UnregisterMachineAsync(
            MachineRegistrationTestData.CreateDeletedMessage(Guid.CreateVersion7()));
        await act.Should().ThrowAsync<NotFoundException>();

        var messages = await _fixture.ReadOutboxMessagesAsync<FileDeleted>();

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task UnregisterMachine_WithFiles_KeepsOtherMachineFiles()
    {
        var machineId = await _fixture.RegisterMachineAsync(MachineRegistrationTestData.CompanyId);
        var otherMachineId = await _fixture.RegisterMachineAsync(MachineRegistrationTestData.CompanyId);
        var ids = await _fixture.SeedFilesAsync(
            MachineRegistrationTestData.CompanyId,
            [
                new FileSeed(
                    FileName: "other", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt, otherMachineId),
                new FileSeed(FileName: "company", Extension: "pdf", SizeBytes: 10, UpdateFileTestData.UploadedAt),
            ]);

        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(machineId));

        var other = await _fixture.LoadFileRowAsync(ids[index: 0]);
        var company = await _fixture.LoadFileRowAsync(ids[index: 1]);

        other.DeletedAt.Should().BeNull();
        company.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithoutRegistration_Throws()
    {
        var message = MachineRegistrationTestData.CreateDeletedMessage(Guid.CreateVersion7());

        var act = () => _fixture.UnregisterMachineAsync(message);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.RegistrationNotFound);
    }

    [Fact]
    public async Task UnregisterMachine_WithDeletedRegistration_Succeeds()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);
        var deleted = MachineRegistrationTestData.CreateDeletedMessage(message.MachineId);
        await _fixture.UnregisterMachineAsync(deleted);

        var act = () => _fixture.UnregisterMachineAsync(deleted);

        await act.Should().NotThrowAsync();
    }
}
