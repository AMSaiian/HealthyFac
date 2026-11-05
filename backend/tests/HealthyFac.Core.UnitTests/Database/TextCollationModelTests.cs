using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Infrastructure.Persistence.Collations;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Core.UnitTests.Database;

public sealed class TextCollationModelTests
{
    private const string JsonbColumnType = "jsonb";
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryTextColumn_UsesCaseInsensitiveCollation()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new CoreDbContext(options);

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
