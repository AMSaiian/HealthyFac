using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Shifts.Data;

public delegate Task<DateOnly> ShiftChangeAction(CoreDatabaseFixture fixture, ShiftTenant tenant);

public delegate Task ShiftRejectedAction(CoreDatabaseFixture fixture, ShiftTenant tenant);

public static class ShiftRealtimeTestData
{
    public static TheoryData<string, ShiftChangeAction> Changes =>
        new()
        {
            {
                "CreateVersion",
                async (fixture, tenant) =>
                {
                    await fixture.CreateVersionAsync(
                        tenant,
                        ShiftTestData.Tomorrow,
                        ShiftTestData.WorkWeek(ShiftTestData.Morning));

                    return ShiftTestData.Tomorrow;
                }
            },
            {
                "UpdateVersion",
                async (fixture, tenant) =>
                {
                    var versionId = await fixture.SeedVersionAsync(
                        tenant.EmployeeId,
                        ShiftTestData.InTwoDays,
                        ShiftTestData.Morning);

                    var rowVersion = await fixture.LoadRowVersionAsync(versionId);

                    await fixture.UpdateVersionAsync(
                        tenant,
                        versionId,
                        ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
                        rowVersion);

                    return ShiftTestData.InTwoDays;
                }
            },
            {
                "DeleteVersion",
                async (fixture, tenant) =>
                {
                    var versionId = await fixture.SeedVersionAsync(
                        tenant.EmployeeId,
                        ShiftTestData.InTwoDays,
                        ShiftTestData.Morning);

                    var rowVersion = await fixture.LoadRowVersionAsync(versionId);

                    await fixture.DeleteVersionAsync(tenant, versionId, rowVersion);

                    return ShiftTestData.InTwoDays;
                }
            },
            {
                "SetOverride",
                async (fixture, tenant) =>
                {
                    await fixture.SetOverrideAsync(
                        tenant, ShiftTestData.Tomorrow, ShiftTestData.Blocks(ShiftTestData.Morning));

                    return ShiftTestData.Tomorrow;
                }
            },
            {
                "DeleteOverride",
                async (fixture, tenant) =>
                {
                    await fixture.SeedOverrideAsync(tenant.EmployeeId, ShiftTestData.Tomorrow, ShiftTestData.Morning);

                    var rowVersion = await fixture.LoadOverrideRowVersionAsync(
                        tenant.EmployeeId, ShiftTestData.Tomorrow);

                    await fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, rowVersion);

                    return ShiftTestData.Tomorrow;
                }
            },
        };

    public static TheoryData<string, ShiftRejectedAction, Type> Rejections =>
        new()
        {
            {
                "CreateVersionOutOfRange",
                (fixture, tenant) => fixture.CreateVersionAsync(
                    tenant,
                    ShiftTestData.Today.AddDays(value: -1),
                    ShiftTestData.WorkWeek(ShiftTestData.Morning)),
                typeof(AppValidationException)
            },
            {
                "UpdateVersionInEffect",
                async (fixture, tenant) =>
                {
                    var versionId = await fixture.SeedVersionAsync(
                        tenant.EmployeeId,
                        ShiftTestData.Today,
                        ShiftTestData.Morning);

                    var rowVersion = await fixture.LoadRowVersionAsync(versionId);

                    await fixture.UpdateVersionAsync(
                        tenant,
                        versionId,
                        ShiftTestData.WorkWeek(ShiftTestData.Afternoon),
                        rowVersion);
                },
                typeof(UnprocessableException)
            },
            {
                "DeleteVersionInEffect",
                async (fixture, tenant) =>
                {
                    var versionId = await fixture.SeedVersionAsync(
                        tenant.EmployeeId,
                        ShiftTestData.Today,
                        ShiftTestData.Morning);

                    var rowVersion = await fixture.LoadRowVersionAsync(versionId);

                    await fixture.DeleteVersionAsync(tenant, versionId, rowVersion);
                },
                typeof(UnprocessableException)
            },
            {
                "SetOverrideOutOfRange",
                (fixture, tenant) => fixture.SetOverrideAsync(
                    tenant,
                    ShiftTestData.Today.AddDays(value: -1),
                    ShiftTestData.Blocks(ShiftTestData.Morning)),
                typeof(AppValidationException)
            },
            {
                "DeleteOverrideMissing",
                (fixture, tenant) => fixture.DeleteOverrideAsync(tenant, ShiftTestData.Tomorrow, rowVersion: 1),
                typeof(NotFoundException)
            },
        };
}
