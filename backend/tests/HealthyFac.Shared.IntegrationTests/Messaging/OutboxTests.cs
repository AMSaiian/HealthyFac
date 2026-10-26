using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class OutboxTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task OutboxAdd_WithActor_StoresActorIdAndCompanyId()
    {
        using var scope = fixture.CreateScope(
            new ActorContext { UserId = OutboxTestData.UserId, CompanyId = OutboxTestData.CompanyId });
        scope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestActorStamped { Text = OutboxTestData.ActorStampedText });

        await scope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();

        var stored = await ReadActorStampedMessageAsync();

        stored.ActorId.Should().Be(OutboxTestData.UserId);
        stored.CompanyId.Should().Be(OutboxTestData.CompanyId);
    }

    [Fact]
    public void OutboxAdd_WithoutActor_ThrowsActorMissing()
    {
        using var scope = fixture.CreateScopeWithoutActor();

        var act = () => scope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestActorStamped { Text = OutboxTestData.ActorStampedText });

        act.Should().Throw<InvalidOperationException>().WithMessage(MessagingErrors.ActorMissing().Message);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAnotherScopeUpdatedSameOutboxRowFirst_RejectsSecondCommit()
    {
        using var publishScope = fixture.CreateScope();
        publishScope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestConcurrentPublish { Text = OutboxTestData.BusinessChangeText });
        await publishScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();
        using var firstScope = fixture.CreateScope();
        using var secondScope = fixture.CreateScope();
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

    private async Task<OutboxMessage> ReadActorStampedMessageAsync()
    {
        using var scope = fixture.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .OutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.Type == OutboxTestData.ActorStampedMessageType);
    }

    private static Task<OutboxMessage> LoadMessageAsync(TestServiceDbContext dbContext)
        => dbContext.OutboxMessages.SingleAsync(message => message.Type == OutboxTestData.ConcurrentMessageType);
}
