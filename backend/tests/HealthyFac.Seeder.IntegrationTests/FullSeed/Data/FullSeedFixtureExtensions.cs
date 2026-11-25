using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.IntegrationTests.FullSeed.Data;

public static class FullSeedFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<IReadOnlyList<SeedCheckFailure>> CheckFullSeedAsync(
            DateTimeOffset anchor,
            CancellationToken cancellationToken
        )
        {
            var folder = new SeedFolder(Path.Combine(AppContext.BaseDirectory, PeopleTestData.SeedFolder));
            var events = SeedPlan.Load(
                SeedParts.Create(fixture.Hosts, SeedRunFixtureExtensions.People(fixture), isTelemetry: false), folder);
            var check = new SeedCheck(
                anchor,
                TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId),
                (request, token) => fixture.Hosts.All.Single(host => host.Name == request.Service)
                    .ValidateAsync(request.Request, token));

            return await check.RunAsync(events.Select(planned => planned.Event).ToList(), cancellationToken);
        }

        public async Task<Dictionary<string, List<string>>> LoadDeliveredTypesAsync()
        {
            var delivered = new Dictionary<string, List<string>>();
            foreach (var host in fixture.Hosts.All)
            {
                var inbox = await fixture.LoadInboxAsync(host);
                delivered[host.Name.Value] = inbox.Select(message => message.Type).ToList();
            }

            return delivered;
        }
    }
}
