using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Realtime.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Realtime;

[Collection(CoreEmployeesCollection.Name)]
public sealed class EmployeeRecipientsTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task FindIdentityUserIdsAsync_WithOneEmployeeWithoutIdentityUser_ReturnsTheOtherTwo()
    {
        var companyId = await _fixture.CreateCompanyAsync(EmployeeRecipientsTestData.CompanyName);
        var first = await ArrangeAsync(companyId, EmployeeRecipientsTestData.FirstEmail);
        var second = await ArrangeAsync(companyId, EmployeeRecipientsTestData.SecondEmail);

        var unprovisionedId = await _fixture.ArrangeUnprovisionedEmployeeAsync(
            companyId,
            Options(EmployeeRecipientsTestData.UnprovisionedEmail));

        using var scope = _fixture.CreateUserScope(companyId);
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var recipients = await dbContext.FindIdentityUserIdsAsync(
            [first.EmployeeId, second.EmployeeId, unprovisionedId],
            TestContext.Current.CancellationToken);

        recipients.Should().HaveCount(expected: 2);
        recipients[first.EmployeeId].Should().Be(first.IdentityUserId);
        recipients[second.EmployeeId].Should().Be(second.IdentityUserId);
        recipients.Should().NotContainKey(unprovisionedId);
    }

    private Task<EmployeeSeed> ArrangeAsync(Guid companyId, string email) =>
        _fixture.ArrangeEmployeeAsync(companyId, Options(email));

    private static EmployeeSeedOptions Options(string email) =>
        new()
        {
            Email = email,
            FirstName = EmployeeRecipientsTestData.FirstName,
            LastName = EmployeeRecipientsTestData.LastName,
        };
}
