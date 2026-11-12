using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class CreateStatusDefinitionTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateStatusDefinition_WithValidRequest_StoresRowWithAuditFields()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.Description);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.MachineRegistrationId.Should().Be(machineId);
        row.Code.Should().Be(StatusDefinitionTestData.Code);
        row.Description.Should().Be(StatusDefinitionTestData.Description);
        row.CreatedAt.Should().NotBe(default(DateTime));
        row.CreatedBy.Should().NotBeEmpty();
        row.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateStatusDefinition_WithVisibleTextAtLimitInsideFormatting_StoresDescription()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.FormattedDescriptionAtVisibleLimit);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.Description.Should().Be(StatusDefinitionTestData.FormattedDescriptionAtVisibleLimit);
        row.Description!.Length.Should().BeGreaterThan(StatusDefinitionTestData.DescriptionLimit);
    }

    [Theory]
    [MemberData(nameof(StatusDefinitionTestData.TakenCodes), MemberType = typeof(StatusDefinitionTestData))]
    public async Task CreateStatusDefinition_WithTakenCode_ThrowsConflict(string caseName, string code)
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            code);

        await act.Should()
            .ThrowAsync<ConflictException>(because: caseName)
            .WithMessage(StatusDefinitionErrors.CodeTaken);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithCodeOfOtherMachine_Succeeds()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var firstMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var secondMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            firstMachineId,
            StatusDefinitionTestData.Code);

        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            secondMachineId,
            StatusDefinitionTestData.Code);

        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.MachineRegistrationId.Should().Be(secondMachineId);
    }

    [Fact]
    public async Task CreateStatusDefinition_WithCodeOfDeletedStatusDefinition_Succeeds()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var firstId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);
        await _fixture.RemoveStatusDefinitionRowAsync(tenant, firstId);

        var secondId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.CodeOtherCase);

        var rows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, StatusDefinitionTestData.Code);

        rows.Select(row => row.Id).Should().BeEquivalentTo([firstId, secondId]);
        rows.Count(row => row.DeletedAt is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task CreateStatusDefinition_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task CreateStatusDefinition_ForDeletedMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }
}
