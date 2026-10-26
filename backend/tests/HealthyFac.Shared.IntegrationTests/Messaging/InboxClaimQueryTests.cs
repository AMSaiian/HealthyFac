using HealthyFac.Shared.Infrastructure.Messaging.Processing;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxClaimQueryTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public void InboxClaimQuery_GeneratedSql_LimitsToOneRowAndSkipsLocked()
    {
        using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();

        var sql = dbContext.BuildInboxClaimQuery(InboxTestData.Now).ToSqlQuery().Sql.TrimEnd();

        sql.Should().Contain(InboxProcessorTestData.LimitSql);
        sql.Should().EndWith(InboxProcessorTestData.ForUpdateSkipLockedSql);
    }
}
