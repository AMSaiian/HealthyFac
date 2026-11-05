using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class InboxClaimQueryTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public void InboxClaimQuery_GeneratedSql_LimitsToOneRowAndSkipsLocked()
    {
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestServiceDbContext>();

        var sql = dbContext.BuildInboxClaimQuery(InboxTestData.Now).ToSqlQuery().Sql.TrimEnd();

        sql.Should().Contain(InboxProcessorTestData.LimitSql);
        sql.Should().EndWith(InboxProcessorTestData.ForUpdateSkipLockedSql);
    }
}
