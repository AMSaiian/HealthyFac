using HealthyFac.Insights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Insights.UnitTests.Database;

internal static class InsightsModelFactory
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    public static IReadOnlyList<IEntityType> GetEntityTypes()
    {
        var options = new DbContextOptionsBuilder<InsightsDbContext>()
            .UseNpgsql(UnusedConnectionString, npgsql => npgsql.UseVector())
            .Options;
        using var dbContext = new InsightsDbContext(options);

        return dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToList();
    }
}
