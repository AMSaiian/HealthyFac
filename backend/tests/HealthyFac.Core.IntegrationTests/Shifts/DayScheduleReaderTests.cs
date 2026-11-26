using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Shifts;

[Collection(CoreShiftsCollection.Name)]
[Feature(TestFeatures.Shifts)]
public sealed class DayScheduleReaderTests(CoreDatabaseFixture fixture)
{
    private const string JanuaryBlock = "08:00-12:00";
    private const string MarchBlock = "09:00-11:00";
    private const string MayBlock = "10:00-12:00";
    private const string JuneBlock = "13:00-17:00";

    private static readonly DateOnly January = new(year: 2027, month: 1, day: 1);
    private static readonly DateOnly March = new(year: 2027, month: 3, day: 1);
    private static readonly DateOnly May = new(year: 2027, month: 5, day: 1);
    private static readonly DateOnly June = new(year: 2027, month: 6, day: 1);
    private static readonly DateOnly AprilMonday = new(year: 2027, month: 4, day: 12);
    private static readonly DateOnly MayMonday = new(year: 2027, month: 5, day: 10);
    private static readonly DateOnly JuneTuesday = new(year: 2027, month: 6, day: 15);

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task LoadAsync_ForOneDayInsideAVersion_LoadsOnlyTheVersionInForce()
    {
        var tenant = await ArrangeVersionsAsync();

        var shifts = await LoadAsync(tenant, AprilMonday, AprilMonday);
        var day = shifts.Resolve(AprilMonday, AprilMonday).Single();

        shifts.Versions.Select(version => version.EffectiveFrom).Should().Equal(March);
        day.Blocks.Should().ContainSingle().Which.Start.Should().Be(new TimeOnly(hour: 9, minute: 0));
    }

    [Fact]
    public async Task LoadAsync_ForARangeWithANewVersionInside_LoadsTheOlderVersionAndTheNewOne()
    {
        var tenant = await ArrangeVersionsAsync();

        var shifts = await LoadAsync(tenant, AprilMonday, JuneTuesday);
        var days = shifts.Resolve(AprilMonday, JuneTuesday);

        shifts.Versions.Select(version => version.EffectiveFrom).Should().Equal(March, June);
        days[0].Blocks.Should().ContainSingle().Which.Start.Should().Be(new TimeOnly(hour: 9, minute: 0));
        days[^1].Blocks.Should().ContainSingle().Which.Start.Should().Be(new TimeOnly(hour: 13, minute: 0));
    }

    [Fact]
    public async Task LoadAsync_WithASoftDeletedNewerVersion_IgnoresIt()
    {
        var tenant = await ArrangeVersionsAsync();

        var shifts = await LoadAsync(tenant, MayMonday, MayMonday);

        shifts.Versions.Select(version => version.EffectiveFrom).Should().Equal(March);
    }

    [Fact]
    public async Task LoadAsync_ForADateBeforeTheFirstVersion_LoadsNoVersionAndTheDayIsOff()
    {
        var tenant = await ArrangeVersionsAsync();
        var beforeFirst = January.AddDays(value: -10);

        var shifts = await LoadAsync(tenant, beforeFirst, beforeFirst);
        var day = shifts.Resolve(beforeFirst, beforeFirst).Single();

        shifts.Versions.Should().BeEmpty();
        day.Blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadWithAllVersionsAsync_Always_LoadsEveryActiveVersion()
    {
        var tenant = await ArrangeVersionsAsync();

        using var scope = _fixture.ScopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<DayScheduleReader>();
        var zone = await reader.GetZoneAsync(tenant.CompanyId, CancellationToken.None);

        var shifts = await reader.LoadWithAllVersionsAsync(
            tenant.EmployeeId,
            zone,
            AprilMonday,
            AprilMonday,
            isTracked: false,
            CancellationToken.None);

        shifts.Versions.Select(version => version.EffectiveFrom).Should().Equal(January, March, June);
    }

    private async Task<ShiftTenant> ArrangeVersionsAsync()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        await _fixture.SeedVersionAsync(tenant.EmployeeId, January, JanuaryBlock);
        await _fixture.SeedVersionAsync(tenant.EmployeeId, March, MarchBlock);
        var deletedId = await _fixture.SeedVersionAsync(tenant.EmployeeId, May, MayBlock);
        await _fixture.SeedVersionAsync(tenant.EmployeeId, June, JuneBlock);

        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
        var deleted = await dbContext.ShiftTemplateVersions.SingleAsync(version => version.Id == deletedId);
        dbContext.ShiftTemplateVersions.Remove(deleted);
        await dbContext.SaveChangesAsync();

        return tenant;
    }

    private async Task<EmployeeShifts> LoadAsync(ShiftTenant tenant, DateOnly from, DateOnly to)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<DayScheduleReader>();
        var zone = await reader.GetZoneAsync(tenant.CompanyId, CancellationToken.None);

        return await reader.LoadAsync(tenant.EmployeeId, zone, from, to, isTracked: false, CancellationToken.None);
    }
}
