using HealthyFac.Core.Application.Employees.SearchEmployeeInvites;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class SearchEmployeeInvitesTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SearchEmployeeInvites_Always_ListsOpenInvitesOfOwnCompanyOnly()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var otherCompanyId = await _fixture.CreateCompanyAsync(EmployeeSearchTestData.OtherCompanyName);
        var acceptedInviteId = await _fixture.CreateInviteWithRoleAsync(
            companyId,
            EmployeeSearchTestData.AcceptedEmail,
            RoleCode.Worker,
            DateTime.UtcNow.AddHours(value: 1));
        await _fixture.AcceptInviteByIdAsync(acceptedInviteId);
        await _fixture.CreateInviteWithRoleAsync(
            companyId,
            EmployeeSearchTestData.PendingEmail,
            RoleCode.Supervisor,
            DateTime.UtcNow.AddHours(value: 1));
        await _fixture.CreateInviteWithRoleAsync(
            otherCompanyId,
            EmployeeSearchTestData.OtherInviteEmail,
            RoleCode.Worker,
            DateTime.UtcNow.AddHours(value: 1));

        var response = await _fixture.SearchEmployeeInvitesAsync(
            companyId,
            EmployeeSearchTestData.InviteRequest(companyId));

        response.TotalCount.Should().Be(expected: 1);
        response.Items.Should().ContainSingle().Which.Email.Should().Be(EmployeeSearchTestData.PendingEmail);
        response.Items[index: 0].RoleCode.Should().Be(RoleCode.Supervisor);
    }

    [Fact]
    public async Task SearchEmployeeInvites_WithExpiredAndValidInvite_FlagsOnlyTheExpiredOne()
    {
        var companyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        await _fixture.CreateInviteWithRoleAsync(
            companyId,
            EmployeeSearchTestData.ExpiredEmail,
            RoleCode.Worker,
            DateTime.UtcNow.AddHours(value: -1));
        await _fixture.CreateInviteWithRoleAsync(
            companyId,
            EmployeeSearchTestData.PendingEmail,
            RoleCode.Worker,
            DateTime.UtcNow.AddHours(value: 1));
        var sort = new GridSort<EmployeeInviteSortColumn>
        {
            Column = EmployeeInviteSortColumn.ExpiresAt,
            Direction = SortDirection.Ascending,
        };

        var response = await _fixture.SearchEmployeeInvitesAsync(
            companyId,
            EmployeeSearchTestData.InviteRequest(companyId, sort));

        response.Items.Select(item => (item.Email, item.IsExpired))
            .Should()
            .Equal(
                (EmployeeSearchTestData.ExpiredEmail, true),
                (EmployeeSearchTestData.PendingEmail, false));
    }
}
