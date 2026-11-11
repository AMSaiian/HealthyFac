using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
public sealed class ShiftReadScopeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetEmployeeDaySchedule_InOwnScopeForOtherEmployee_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);

        var act = () => _fixture.ReadAsAsync<GetEmployeeDayScheduleRequest, DayScheduleResponse>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new GetEmployeeDayScheduleRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = other.EmployeeId,
                Date = ShiftTestData.Today,
                Scope = AccessScope.Own,
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListShiftTemplateVersions_InOwnScopeForOtherEmployee_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);

        var act = () =>
            _fixture.ReadAsAsync<ListShiftTemplateVersionsRequest, IReadOnlyList<ShiftTemplateVersionResponse>>(
                tenant.CompanyId,
                tenant.IdentityUserId,
                new ListShiftTemplateVersionsRequest
                {
                    CompanyId = tenant.CompanyId,
                    EmployeeId = other.EmployeeId,
                    Scope = AccessScope.Own,
                });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListShiftOverrides_InOwnScopeForOtherEmployee_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);

        var act = () => _fixture.ReadAsAsync<ListShiftOverridesRequest, IReadOnlyList<ShiftOverrideResponse>>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ListShiftOverridesRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = other.EmployeeId,
                From = ShiftTestData.Today,
                To = ShiftTestData.Tomorrow,
                Scope = AccessScope.Own,
            });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListShiftOverrides_InOwnScopeForOwnEmployee_ReturnsTheOverrides()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SetOverrideAsync(tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));

        var overrides = await _fixture.ReadAsAsync<ListShiftOverridesRequest, IReadOnlyList<ShiftOverrideResponse>>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ListShiftOverridesRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                From = ShiftTestData.Today,
                To = ShiftTestData.InTwoDays,
                Scope = AccessScope.Own,
            });

        overrides.Should().ContainSingle().Which.Date.Should().Be(ShiftTestData.Tomorrow);
    }

    [Fact]
    public async Task ListShiftTemplateVersions_InCompanyScopeForOtherEmployee_ReturnsSevenDaysForEachVersion()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        var other = await _fixture.ArrangeOtherEmployeeAsync(tenant.CompanyId, ShiftTestData.OtherEmail);
        _fixture.Clock.Freeze(ShiftTestData.NowAt(time: "09:30"));
        await _fixture.SeedVersionAsync(other.EmployeeId, ShiftTestData.Tomorrow, ShiftTestData.Morning);

        var versions = await _fixture
            .ReadAsAsync<ListShiftTemplateVersionsRequest, IReadOnlyList<ShiftTemplateVersionResponse>>(
                tenant.CompanyId,
                tenant.IdentityUserId,
                new ListShiftTemplateVersionsRequest
                {
                    CompanyId = tenant.CompanyId,
                    EmployeeId = other.EmployeeId,
                    Scope = AccessScope.Company,
                });

        var version = versions.Should().ContainSingle().Which;
        version.Days.Should().HaveCount(expected: 7);
        version.Days.Count(day => day.Blocks.Count == 1).Should().Be(expected: 5);
    }

    [Fact]
    public async Task ListShiftOverrides_WithRangeLongerThan62Days_FailsValidation()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();

        var act = () => _fixture.ReadAsAsync<ListShiftOverridesRequest, IReadOnlyList<ShiftOverrideResponse>>(
            tenant.CompanyId,
            tenant.IdentityUserId,
            new ListShiftOverridesRequest
            {
                CompanyId = tenant.CompanyId,
                EmployeeId = tenant.EmployeeId,
                From = ShiftTestData.Today,
                To = ShiftTestData.Today.AddDays(ShiftValidationConstants.MaxOverrideRangeDays),
                Scope = AccessScope.Company,
            });

        await act.Should().ThrowAsync<AppValidationException>();
    }
}
