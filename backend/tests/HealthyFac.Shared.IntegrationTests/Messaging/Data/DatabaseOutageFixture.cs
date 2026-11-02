using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// The transport fixture with a retry strategy on the test service context and an interceptor that fails the
/// first inbox inserts. Only the classes that use this fixture see the failures.
/// </summary>
public sealed class DatabaseOutageFixture : TransportFixture
{
    private readonly InboxInsertFailureInterceptor _interceptor = new(DatabaseOutageTestData.FailedCommandCount);

    public int InjectedFailureCount => _interceptor.InjectedCount;

    protected override void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(
                connectionString,
                npgsql => npgsql.EnableRetryOnFailure(
                    DatabaseOutageTestData.RetryMaxCount,
                    DatabaseOutageTestData.RetryMaxDelay,
                    errorCodesToAdd: null))
            .AddInterceptors(_interceptor);
    }
}
