using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Testing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Document.UnitTests.Database;

public sealed class RowVersionModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryRowVersion_IsVersionedPropertyOnXmin()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new DocumentDbContext(options);

        var wrongRowVersions = RowVersionModelChecks.FindWrongRowVersions(
            dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes());

        wrongRowVersions.Should().BeEmpty();
    }
}
