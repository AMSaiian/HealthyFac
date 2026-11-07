using HealthyFac.Shared.Infrastructure.Persistence.Collations;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Telemetry.UnitTests.Database;

public sealed class TextCollationModelTests
{
    private const string JsonbColumnType = "jsonb";
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryTextColumn_UsesCaseInsensitiveCollation()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new TelemetryDbContext(options);

        var wrongProperties = dbContext.GetService<IDesignTimeModel>()
            .Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(string) && property.GetColumnType() != JsonbColumnType)
            .Where(property => property.GetCollation() != CollationNames.CaseInsensitive)
            .Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}")
            .ToList();

        wrongProperties.Should().BeEmpty();
    }
}
