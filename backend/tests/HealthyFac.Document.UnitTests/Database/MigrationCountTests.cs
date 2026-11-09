using System.Reflection;
using HealthyFac.Document.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthyFac.Document.UnitTests.Database;

public sealed class MigrationCountTests
{
    private const string InitialCreateSuffix = "_InitialCreate";

    [Fact]
    public void DocumentMigrations_InAssembly_AreExactlyInitialCreate()
    {
        var migrationIds = DocumentInfrastructureAssembly.Instance
            .GetTypes()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>())
            .OfType<MigrationAttribute>()
            .Select(attribute => attribute.Id)
            .ToList();

        migrationIds.Should().ContainSingle().Which.Should().EndWith(InitialCreateSuffix);
    }
}
