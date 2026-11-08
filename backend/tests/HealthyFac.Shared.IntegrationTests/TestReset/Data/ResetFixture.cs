using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace HealthyFac.Shared.IntegrationTests.TestReset.Data;

/// <summary>
/// A collection fixture with one PostgreSQL database and one RabbitMQ broker. It shows the shape of every collection
/// fixture: reference data that the reset keeps, base data that the reset restores, and a queue that the reset empties.
/// </summary>
public sealed class ResetFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();
    private readonly RabbitMqTestBroker _broker = new();

    private DatabaseResetter _resetter = null!;

    public string BrokerConnectionString => _broker.ConnectionString;

    public ResetDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<ResetDbContext>().UseNpgsql(_database.ConnectionString).Options);

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
        await _broker.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ReferenceItems.AddRange(
            ResetTestData.ReferenceItemNames.Select(name => new ReferenceItem { Name = name }));
        await dbContext.SaveChangesAsync();
        await SeedBaseDataAsync(dbContext);

        _resetter = await DatabaseResetter.CreateAsync(_database.ConnectionString, dbContext, typeof(ReferenceItem));

        await using var connection = await new ConnectionFactory { Uri = new Uri(_broker.ConnectionString) }
            .CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(ResetTestData.QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public async ValueTask ResetAsync()
    {
        await _resetter.ResetAsync();
        await _broker.PurgeQueuesAsync(ResetTestData.QueueName);

        await using var dbContext = CreateDbContext();
        await SeedBaseDataAsync(dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await _broker.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static async Task SeedBaseDataAsync(ResetDbContext dbContext)
    {
        dbContext.BaseItems.Add(new BaseItem { Name = ResetTestData.BaseItemName });
        await dbContext.SaveChangesAsync();
    }
}
