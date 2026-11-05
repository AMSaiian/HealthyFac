using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes;

public sealed class CreateMachineErrorCodeTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateMachineErrorCode_WithValidRequest_StoresRowWithAuditFields()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);

        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code,
            MachineErrorCodeTestData.Description);

        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.MachineId.Should().Be(machineId);
        row.Code.Should().Be(MachineErrorCodeTestData.Code);
        row.Description.Should().Be(MachineErrorCodeTestData.Description);
        row.CreatedAt.Should().NotBe(default(DateTime));
        row.CreatedBy.Should().NotBeEmpty();
        row.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateMachineErrorCode_WithVisibleTextAtLimitInsideFormatting_StoresDescription()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);

        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code,
            MachineErrorCodeTestData.FormattedDescriptionAtVisibleLimit);

        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.Description.Should().Be(MachineErrorCodeTestData.FormattedDescriptionAtVisibleLimit);
        row.Description!.Length.Should().BeGreaterThan(MachineErrorCodeTestData.DescriptionLimit);
    }

    [Fact]
    public async Task CreateMachineErrorCode_WithDuplicateCode_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MachineErrorCodeErrors.CodeTaken);
    }

    [Fact]
    public async Task CreateMachineErrorCode_WithCodeInOtherCase_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        var act = () => _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.CodeOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MachineErrorCodeErrors.CodeTaken);
    }

    [Fact]
    public async Task CreateMachineErrorCode_WithCodeOfOtherMachine_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var firstMachineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var secondMachineId = await _fixture.CreateErrorCodeMachineAsync(
            tenant,
            MachineErrorCodeTestData.SecondMachineName,
            MachineErrorCodeTestData.SecondMachineSerialNumber);
        await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            firstMachineId,
            MachineErrorCodeTestData.Code);

        var errorCodeId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            secondMachineId,
            MachineErrorCodeTestData.Code);

        var row = await _fixture.LoadMachineErrorCodeRowAsync(errorCodeId);

        row.MachineId.Should().Be(secondMachineId);
    }

    [Fact]
    public async Task CreateMachineErrorCode_WithCodeOfDeletedErrorCode_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        var firstId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);
        await _fixture.RemoveMachineErrorCodeRowAsync(tenant, firstId);

        var secondId = await _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.CodeOtherCase);

        var rows = await _fixture.LoadMachineErrorCodeRowsAsync(machineId, MachineErrorCodeTestData.Code);

        rows.Select(row => row.Id).Should().BeEquivalentTo([firstId, secondId]);
        rows.Count(row => row.DeletedAt is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task CreateMachineErrorCode_InForeignDepartment_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);

        var act = () => _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            [tenant.SecondDepartmentId],
            machineId,
            MachineErrorCodeTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task CreateMachineErrorCode_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(otherTenant);

        var act = () => _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task CreateMachineErrorCode_ForDeletedMachine_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateErrorCodeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, machineId);

        var act = () => _fixture.CreateMachineErrorCodeAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            machineId,
            MachineErrorCodeTestData.Code);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }
}
