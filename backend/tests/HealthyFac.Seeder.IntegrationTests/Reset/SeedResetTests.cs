using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.Reset.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.Moments;
using HealthyFac.Testing.Features;
using Npgsql;

namespace HealthyFac.Seeder.IntegrationTests.Reset;

[Collection(SeederCollection.Name)]
[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Persistence)]
public sealed class SeedResetTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheResetArgument_SeedsAgainWithTheDatesOfTheNewAnchorAndKeepsTheRequiredData()
    {
        var laterAnchor = FullSeedTestData.SaturdayAnchor.AddDays(SeedResetTestData.LaterAnchorDays);
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        var rolesBefore = await _fixture.LoadRoleIdsAsync();
        var unitsBefore = await _fixture.LoadUnitIdsAsync();
        var usersBefore = await _fixture.CountKeycloakUsersAsync();
        await _fixture.AddExtraMemberAsync(TestContext.Current.CancellationToken);
        await _fixture.AddExtraBlobAsync(TestContext.Current.CancellationToken);

        var reset = await _fixture.RunResetAsync(laterAnchor, TestContext.Current.CancellationToken);

        var rolesAfter = await _fixture.LoadRoleIdsAsync();
        var unitsAfter = await _fixture.LoadUnitIdsAsync();
        var usersAfter = await _fixture.CountKeycloakUsersAsync();
        var blobCount = await _fixture.CountBlobsAsync();
        var employees = await _fixture.LoadEmployeesAsync();
        var company = await _fixture.LoadCompanyAsync();

        var expectedCreatedAt = SeedCalendar.ToUtc(
            RelativeMoment.Parse(SeedResetTestData.CompanyCreatedAt),
            laterAnchor,
            TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId));

        reset.ExitCode.Should().Be(expected: 0);
        rolesAfter.Should().Equal(rolesBefore);
        unitsAfter.Should().Equal(unitsBefore);
        usersAfter.Should().Be(usersBefore);
        blobCount.Should().Be(SeedResetTestData.BlobCount);
        employees.Should().HaveCount(FullSeedTestData.EmployeeCount);
        company.CreatedAt.Should().BeCloseTo(expectedCreatedAt.UtcDateTime, TimeSpan.FromSeconds(seconds: 1));
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheResetArgumentAndAnOpenConnection_ExitsWithOneNamesTheDatabaseAndWritesNothing()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        var before = await _fixture.TakeSnapshotAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionStringOf(service: "core"))
        {
            Pooling = false,
        }.ConnectionString;

        await using var extra = new NpgsqlConnection(connectionString);
        await extra.OpenAsync(TestContext.Current.CancellationToken);

        var reset = await _fixture.RunResetAsync(
            FullSeedTestData.SaturdayAnchor.AddDays(SeedResetTestData.LaterAnchorDays),
            TestContext.Current.CancellationToken);

        var after = await _fixture.TakeSnapshotAsync();
        reset.ExitCode.Should().Be(expected: 1);
        reset.Output.Should().Contain(SeedResetTestData.CoreDatabase).And.Contain(SeedResetTestData.StopHint);
        after.Should().BeEquivalentTo(before);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterAResetThatStoppedAfterTheMarkerMessage_ReportsTheMissingHistoryInModeTwo()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        await _fixture.AddReceivedMessageAsync(SeedCompletionTestData.MarkerMessageId);
        await _fixture.AddReceivedMessageAsync(SeedCompletionTestData.OtherMessageId);
        using var stop = new CancellationTokenSource();
        var act = () => _fixture.RunResetStoppedAfterTheMarkerMessageAsync(stop);
        await act.Should().ThrowAsync<OperationCanceledException>();

        var next = await _fixture.RunJobAsync(
            [SeedCompletionTestData.TelemetryArgument], TestContext.Current.CancellationToken);

        next.ExitCode.Should().Be(expected: 1);
        next.Output.Should().Contain(SeedCompletionTestData.NoCompleteHistoryText);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterAResetThatStoppedAfterItsFirstWrite_ReportsAnIncompleteSeedAndTheResetHint()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        using var stop = new CancellationTokenSource();
        var act = () => _fixture.RunResetStoppedAfterTheMarkerAsync(stop);
        await act.Should().ThrowAsync<OperationCanceledException>();

        var next = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        next.ExitCode.Should().Be(expected: 1);

        next.Output.Should()
            .Contain(SeedCompletionTestData.IncompleteText)
            .And.Contain(SeedCompletionTestData.ResetHint);
    }
}
