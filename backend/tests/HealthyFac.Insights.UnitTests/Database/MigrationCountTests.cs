using System.Reflection;
using HealthyFac.Insights.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthyFac.Insights.UnitTests.Database;

public sealed class MigrationCountTests
{
    private const string InitialCreateSuffix = "_InitialCreate";

    [Fact]
    public void InsightsMigrations_InAssembly_AreOnlyInitialCreate()
    {
        var migrationIds = InsightsInfrastructureAssembly.Instance
            .GetTypes()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>())
            .OfType<MigrationAttribute>()
            .Select(attribute => attribute.Id)
            .ToList();

        migrationIds.Should().ContainSingle().Which.Should().EndWith(InitialCreateSuffix);
    }
}
