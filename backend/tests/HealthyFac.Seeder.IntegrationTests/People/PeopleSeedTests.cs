using HealthyFac.Core.Domain.Employees;
using HealthyFac.Seeder.IntegrationTests.Accounts.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.SeedFiles;

namespace HealthyFac.Seeder.IntegrationTests.People;

[Collection(SeederCollection.Name)]
public sealed class PeopleSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact]
    public async Task RunAsync_WithTheRealCompanyFile_SeedsTheCompanyThePeopleTheInvitesAndTheShifts()
    {
        var context = await _fixture.RunPeopleSeedAsync(TestContext.Current.CancellationToken);

        var employees = await _fixture.LoadEmployeesAsync();
        var invites = await _fixture.LoadInvitesAsync();
        var versionCount = await _fixture.CountShiftTemplateVersionsAsync();
        var overrides = await _fixture.LoadOverridesAsync();
        var openCount = await _fixture.CountOpenMessagesAsync();
        var mails = await _fixture.Mailpit.GetMessagesAsync();
        var failedCount = await _fixture.CountFailedInboxMessagesAsync();
        var company = await _fixture.LoadCompanyAsync();
        var tarasInvite = invites.Single(invite => invite.Email == PeopleTestData.TarasEmail);
        employees.Should()
            .HaveCount(PeopleTestData.PersonCount)
            .And.OnlyContain(employee => employee.Status == EmployeeStatus.Active);
        invites.Count(invite => invite.AcceptedAt is not null).Should().Be(PeopleTestData.PersonCount);
        invites.Count(invite => invite.AcceptedAt is null && invite.ExpiresAt > PeopleTestData.Anchor.UtcDateTime)
            .Should()
            .Be(PeopleTestData.OpenInviteCount);
        versionCount.Should().Be(PeopleTestData.PersonCount);
        overrides.Should().HaveCount(PeopleTestData.OverrideCount);
        openCount.Should().Be(expected: 0);
        failedCount.Should().Be(expected: 0);
        mails.Count(mail =>
                mail.To.Any(address => address.EndsWith(PeopleTestData.InviteEmailDomain, StringComparison.Ordinal)))
            .Should()
            .Be(PeopleTestData.InviteEmailCount);
        context.GetId(key: "company").Should().Be(PeopleTestData.CompanyId);
        company.Name.Should().Be(PeopleTestData.CompanyName);
        company.TimeZoneId.Should().Be(PeopleTestData.ZoneId);
        tarasInvite.CreatedAt.Should().BeCloseTo(TarasInvitedAtUtc(), PeopleTestData.MomentTolerance);
        tarasInvite.CreatedBy.Should().Be(context.GetId(PeopleTestData.NadiiaUserKey));
    }

    [Fact]
    public async Task RunAsync_WithTheRealCompanyFile_LetsEachAccountSignInWithTheDemoPassword()
    {
        var context = await _fixture.RunPeopleSeedAsync(TestContext.Current.CancellationToken);
        var employees = await _fixture.LoadEmployeesAsync();

        foreach (var employee in employees)
        {
            var claims = await _fixture.SignInAsync(employee.Email, PeopleTestData.Password);
            claims[propertyName: "sub"]!.GetValue<string>().Should().Be(employee.IdentityUserId.ToString());
            claims.ToJsonString().Should().Contain(PeopleTestData.CompanyId.ToString());
            claims[PeopleTestData.EmployeeIdClaim]!.GetValue<string>().Should().Be(employee.Id.ToString());
            var hasSupervisorPermission = claims.ToJsonString()
                .Contains(PeopleTestData.SupervisorPermission, StringComparison.Ordinal);
            hasSupervisorPermission.Should().Be(PeopleTestData.SupervisorEmails.Contains(employee.Email));
        }
    }

    [Fact]
    public async Task Check_OnTheRealCompanyFile_ReturnsNoFailure()
    {
        var events = await _fixture.LoadPeopleEventsAsync();
        var check = new SeedCheck(
            PeopleTestData.Anchor,
            TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId),
            (request, token) => _fixture.Hosts.Core.ValidateAsync(request.Request, token));

        var failures = await check.RunAsync(
            events.Select(planned => planned.Event).ToList(), TestContext.Current.CancellationToken);

        failures.Should().BeEmpty();
    }

    private static DateTime TarasInvitedAtUtc()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId);
        var localDay = TimeZoneInfo.ConvertTime(PeopleTestData.Anchor, zone)
            .Date
            .AddDays(-PeopleTestData.TarasInvitedDaysBefore);
        var local = localDay + PeopleTestData.TarasInvitedTime.ToTimeSpan();

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}
