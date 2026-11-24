using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.IntegrationTests.Hosting;

public static class SeedRunFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<SeedContext> RunPartsAsync(
            Func<SeederFixture, IReadOnlyList<ISeedPart>> createParts,
            CancellationToken cancellationToken,
            DateTimeOffset? anchor = null,
            string? seedFolder = null
        )
        {
            var seedAnchor = anchor ?? PeopleTestData.Anchor;
            var zone = TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId);
            var folder = new SeedFolder(
                seedFolder ?? Path.Combine(AppContext.BaseDirectory, PeopleTestData.SeedFolder));
            var events = SeedPlan.Load(createParts(fixture), folder);
            fixture.TrackOrganization(PeopleTestData.CompanyId);
            var context = new SeedContext { Anchor = seedAnchor, Zone = zone };
            var relay = fixture.Hosts.CreateRelay();
            var runner = new TimelineRunner(
                fixture.Clock,
                seedAnchor,
                zone,
                async token =>
                {
                    await Task.WhenAll(fixture.Hosts.All.Select(host => host.InitializeAsync(token)));
                    await fixture.CreateResettersAsync();
                },
                relay.RunAsync,
                _ => { });

            await runner.RunAsync(events, context, cancellationToken);

            return context;
        }

        public Task<SeedContext> RunIngestSeedAsync(CancellationToken cancellationToken) =>
            fixture.RunPartsAsync(
                current => [People(current), new IngestPart(current.Hosts)],
                cancellationToken);

        public Task<SeedContext> RunMachinesSeedAsync(CancellationToken cancellationToken) =>
            fixture.RunPartsAsync(
                current =>
                [
                    People(current),
                    new IngestPart(current.Hosts),
                    new MachinesPart(current.Hosts),
                ],
                cancellationToken);

        public Task<SeedContext> RunFullSeedAsync(
            CancellationToken cancellationToken,
            DateTimeOffset? anchor = null,
            string? seedFolder = null
        ) =>
            fixture.RunPartsAsync(
                current => SeedParts.Create(current.Hosts, People(current), isTelemetry: false),
                cancellationToken,
                anchor,
                seedFolder);
    }

    public static PeoplePart People(SeederFixture fixture) =>
        new(fixture.Hosts, new KeycloakAccounts(fixture.Hosts.Auth));
}
