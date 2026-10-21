using HealthyFac.Shared.Application.Time;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public interface ITestServiceDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}

public sealed class TestServiceDbContext(DbContextOptions<TestServiceDbContext> options)
    : DbContext(options), ITestServiceDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.ToSnakeCaseConventions();
    }
}

public sealed class FixedDateProvider : IDateProvider
{
    public DateTime GetUtc() => InboxTestData.Now;
}
