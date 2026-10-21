using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class OutboxTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task SaveChangesAsync_WhenAnotherScopeUpdatedSameOutboxRowFirst_RejectsSecondCommit()
    {
        using var publishScope = fixture.Services.CreateScope();
        publishScope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestConcurrentPublish { Text = OutboxTestData.BusinessChangeText });
        await publishScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();
        using var firstScope = fixture.Services.CreateScope();
        using var secondScope = fixture.Services.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestServiceDbContext>();
        var firstMessage = await LoadMessageAsync(firstContext);
        var secondMessage = await LoadMessageAsync(secondContext);
        firstContext.Entry(firstMessage).Property(message => message.AttemptCount).CurrentValue =
            OutboxTestData.FirstAttemptCount;
        await firstContext.SaveChangesAsync();
        secondContext.Entry(secondMessage).Property(message => message.AttemptCount).CurrentValue =
            OutboxTestData.SecondAttemptCount;

        var act = () => secondContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private static Task<OutboxMessage> LoadMessageAsync(TestServiceDbContext dbContext)
        => dbContext.OutboxMessages.SingleAsync(message => message.Type == OutboxTestData.ConcurrentMessageType);
}
