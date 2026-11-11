using HealthyFac.Document.Application.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class AutomaticTieTests(DocumentDatabaseFixture fixture)
{
    private static readonly Guid CompanyId = MachineRegistrationTestData.CompanyId;

    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithUntiedFile_TiesAndStoresOneMessage(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var file = await _fixture.LoadFileRowAsync(fileId);
        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        file.MachineId.Should().Be(machineId);
        file.MachineChangedAt.Should().NotBeNull();
        messages.Should().ContainSingle().Which.MachineId.Should().Be(machineId);
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithTiedFile_ChangesNothing(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var otherMachineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();
        await _fixture.TieDirectlyAsync(fileId, otherMachineId, AutomaticTieTestData.QueuedAt.AddHours(value: -1));

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var file = await _fixture.LoadFileRowAsync(fileId);
        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        file.MachineId.Should().Be(otherMachineId);
        messages.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithDeletedFile_ChangesNothing(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();
        await _fixture.DeleteCompanyFileAsync(CompanyId, new DeleteFileRequest { CompanyId = CompanyId, Id = fileId });

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var file = await _fixture.LoadFileRowAsync(fileId);
        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        file.MachineId.Should().BeNull();
        messages.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithManualUntieAfterQueue_ChangesNothing(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();
        await _fixture.UntieDirectlyAsync(fileId, AutomaticTieTestData.QueuedAt.AddMinutes(value: 1));

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var file = await _fixture.LoadFileRowAsync(fileId);
        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        file.MachineId.Should().BeNull();
        messages.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithoutRegistration_Throws(AutomaticTieSource source)
    {
        var fileId = await SeedCompanyFileAsync();

        var act = () => _fixture.DeliverAutomaticTieAsync(
            source, fileId, CompanyId, Guid.CreateVersion7(), AutomaticTieTestData.QueuedAt);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithDeletedRegistration_CompletesWithoutChange(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();
        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(machineId));

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var file = await _fixture.LoadFileRowAsync(fileId);
        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        file.MachineId.Should().BeNull();
        messages.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(AutomaticTieTestData.Sources), MemberType = typeof(AutomaticTieTestData))]
    public async Task AutomaticTie_WithRepeatedDelivery_StoresOneMessage(AutomaticTieSource source)
    {
        var machineId = await _fixture.RegisterMachineAsync(CompanyId);
        var fileId = await SeedCompanyFileAsync();

        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);
        await _fixture.DeliverAutomaticTieAsync(source, fileId, CompanyId, machineId, AutomaticTieTestData.QueuedAt);

        var messages = await _fixture.ReadOutboxMessagesAsync<FileMachineChanged>();

        messages.Should().ContainSingle();
    }

    private async Task<Guid> SeedCompanyFileAsync()
    {
        var ids = await _fixture.SeedFilesAsync(CompanyId, [UpdateFileTestData.CompanyFile()]);

        return ids[index: 0];
    }
}
