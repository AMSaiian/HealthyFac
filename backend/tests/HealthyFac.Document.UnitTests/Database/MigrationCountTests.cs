using System.Reflection;
using HealthyFac.Document.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthyFac.Document.UnitTests.Database;

public sealed class MigrationCountTests
{
    private const string InitialCreateSuffix = "_InitialCreate";
    private const string FileIngestSuffix = "_FileIngest";
    private const string FileDescriptionRichTextSuffix = "_FileDescriptionRichText";

    [Fact]
    public void DocumentMigrations_InAssembly_AreInitialCreateFileIngestAndFileDescriptionRichText()
    {
        var migrationIds = DocumentInfrastructureAssembly.Instance
            .GetTypes()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>())
            .OfType<MigrationAttribute>()
            .Select(attribute => attribute.Id)
            .Order()
            .ToList();

        migrationIds.Should().HaveCount(expected: 3);
        migrationIds[index: 0].Should().EndWith(InitialCreateSuffix);
        migrationIds[index: 1].Should().EndWith(FileIngestSuffix);
        migrationIds[index: 2].Should().EndWith(FileDescriptionRichTextSuffix);
    }
}
