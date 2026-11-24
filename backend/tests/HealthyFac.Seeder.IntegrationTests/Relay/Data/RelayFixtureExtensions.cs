using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Relay.Data;

public static class RelayFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        /// <summary>Creates a company and a machine through the Core use case, so the outbox holds MachineCreated.
        /// </summary>
        public async Task<(Guid CompanyId, Guid MachineId)> CreateMachineAsync()
        {
            fixture.EnterNextMoment(item: "machine-create");
            using (var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor))
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                dbContext.Companies.Add(
                    new Company
                    {
                        Name = RelayTestData.CompanyName,
                        TimeZoneId = RelayTestData.CompanyTimeZoneId,
                    });
                await dbContext.SaveChangesAsync();
                var companyId = await dbContext.Companies.Select(company => company.Id).SingleAsync();
                var actor = RelayTestData.ActorOf(companyId);
                var response = await fixture.Hosts.Core.SendAsync<CreateMachineRequest, CreateMachineResponse>(
                    actor,
                    new CreateMachineRequest
                    {
                        Name = RelayTestData.MachineName,
                        SerialNumber = RelayTestData.SerialNumber,
                        CompanyId = companyId,
                        ExpectedGoLiveOn = RelayTestData.GoLiveOn,
                    },
                    CancellationToken.None);

                return (companyId, response.Id);
            }
        }

        /// <summary>Adds MachineStateChanged messages to the Core outbox in one save.</summary>
        public async Task<IReadOnlyList<MachineStateChanged>> QueueStateChangesAsync(
            Guid companyId,
            Guid machineId,
            int count
        )
        {
            var moment = fixture.EnterNextMoment(item: "state-changes");
            var messages = Enumerable.Range(start: 1, count)
                .Select(index => new MachineStateChanged
                {
                    MachineId = machineId,
                    CompanyId = companyId,
                    State = index % 2 == 0 ? MachineStateValue.Operational : MachineStateValue.InMaintenance,
                    StartedAt = moment.UtcDateTime.AddMinutes(index),
                })
                .ToList();

            await fixture.AddToCoreOutboxAsync(companyId, messages);

            return messages;
        }

        public async Task AddToCoreOutboxAsync(Guid companyId, IEnumerable<MachineStateChanged> messages)
        {
            using var scope = fixture.Hosts.Core.CreateScope(RelayTestData.ActorOf(companyId));
            var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
            foreach (var message in messages)
            {
                outbox.Add(message);
            }

            await scope.ServiceProvider.GetRequiredService<CoreDbContext>().SaveChangesAsync();
        }

        public async Task<List<OutboxMessage>> LoadOutboxAsync(ServiceHost host)
        {
            using var scope = host.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<IOutboxDbContext>()
                .OutboxMessages
                .AsNoTracking()
                .OrderBy(message => message.OccurredOn)
                .ToListAsync();
        }

        public async Task<List<InboxMessage>> LoadInboxAsync(ServiceHost host)
        {
            using var scope = host.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<IInboxDbContext>()
                .InboxMessages
                .AsNoTracking()
                .OrderBy(message => message.ReceivedOn)
                .ToListAsync();
        }

        public async Task<int> CountOpenMessagesAsync()
        {
            var openCount = 0;
            foreach (var host in fixture.Hosts.All)
            {
                var outbox = await fixture.LoadOutboxAsync(host);
                var inbox = await fixture.LoadInboxAsync(host);
                openCount += outbox.Count(message => message.ProcessedOn is null)
                    + inbox.Count(message => message.ProcessedOn is null);
            }

            return openCount;
        }
    }

    public static MachineStateChanged ReadStateChange(this InboxMessage message) =>
        System.Text.Json.JsonSerializer.Deserialize<MachineStateChanged>(
            message.Payload,
            MessagingJsonOptions.Instance)!;
}
