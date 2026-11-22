using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.IntegrationTests.Accounts.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;

namespace HealthyFac.Seeder.IntegrationTests.Accounts;

[Collection(SeederCollection.Name)]
public sealed class KeycloakAccountsTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact]
    public async Task CreateOrganizationAsync_WithACompany_MakesTheOrganizationFindableByTheCompanyId()
    {
        _fixture.TrackOrganization(AccountsTestData.CompanyId);
        _fixture.EnterNextMoment(item: "accounts");
        var accounts = new KeycloakAccounts(_fixture.Hosts.Auth);

        var organizationId = await accounts.CreateOrganizationAsync(
            AccountsTestData.CompanyId,
            AccountsTestData.OrganizationName,
            TestContext.Current.CancellationToken);

        var found = await _fixture.FindOrganizationAsync(AccountsTestData.CompanyId);
        found.Should().Be(organizationId);
    }

    [Fact]
    public async Task CreateUserAsync_WithAPassword_CreatesAUserThatSignsIn()
    {
        _fixture.EnterNextMoment(item: "accounts");
        var accounts = new KeycloakAccounts(_fixture.Hosts.Auth);

        var userId = await accounts.CreateUserAsync(
            AccountsTestData.EmployeeId,
            AccountsTestData.Email,
            AccountsTestData.FirstName,
            AccountsTestData.LastName,
            AccountsTestData.Password,
            TestContext.Current.CancellationToken);

        var foundId = await _fixture.FindUserAsync(AccountsTestData.EmployeeId);
        var hasPassword = await _fixture.HasPasswordAsync(userId);
        var claims = await _fixture.SignInAsync(AccountsTestData.Email, AccountsTestData.Password);
        foundId.Should().Be(userId);
        hasPassword.Should().BeTrue();
        claims[propertyName: "sub"]!.GetValue<string>().Should().Be(userId.ToString());
    }

    [Fact]
    public async Task DeleteOrganizationWithMembersAsync_WithElevenMembers_RemovesEveryMemberAndTheOrganization()
    {
        _fixture.EnterNextMoment(item: "accounts");
        var accounts = new KeycloakAccounts(_fixture.Hosts.Auth);
        var companyId = AccountsTestData.CompanyId;
        var organizationId = await accounts.CreateOrganizationAsync(
            companyId,
            AccountsTestData.OrganizationName,
            TestContext.Current.CancellationToken);
        var employeeIds = Enumerable.Range(start: 0, AccountsTestData.MemberCount)
            .Select(_ => Guid.CreateVersion7())
            .ToList();
        foreach (var employeeId in employeeIds)
        {
            var userId = await accounts.CreateUserAsync(
                employeeId,
                $"{employeeId:N}@kalyna-works.test",
                AccountsTestData.FirstName,
                AccountsTestData.LastName,
                AccountsTestData.Password,
                TestContext.Current.CancellationToken);
            await _fixture.AddMemberAsync(organizationId, userId);
        }

        await accounts.DeleteOrganizationWithMembersAsync(companyId, TestContext.Current.CancellationToken);

        var organization = await _fixture.FindOrganizationAsync(companyId);
        var remainingUsers = await _fixture.CountUsersAsync(employeeIds);
        organization.Should().BeNull();
        remainingUsers.Should().Be(expected: 0);
    }
}
