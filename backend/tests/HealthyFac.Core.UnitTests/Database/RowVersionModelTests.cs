using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Core.UnitTests.Database;

public sealed class RowVersionModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryRowVersion_IsVersionedPropertyOnXmin()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new CoreDbContext(options);

        var wrongRowVersions = RowVersionModelChecks.FindWrongRowVersions(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        wrongRowVersions.Should().BeEmpty();
    }
}
