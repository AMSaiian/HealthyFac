using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Telemetry.UnitTests.Database;

public sealed class RowVersionModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryRowVersion_IsVersionedPropertyOnXmin()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new TelemetryDbContext(options);

        var wrongRowVersions = RowVersionModelChecks.FindWrongRowVersions(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        wrongRowVersions.Should().BeEmpty();
    }
}
