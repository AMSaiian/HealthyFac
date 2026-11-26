using HealthyFac.Seeder.Reset;
using HealthyFac.Seeder.UnitTests.Reset.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Seeder.UnitTests.Reset;

[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Persistence)]
public sealed class TruncateTablesTests
{
    [Theory(Explicit = true)]
    [MemberData(nameof(TruncateTablesTestData.Contexts), MemberType = typeof(TruncateTablesTestData))]
    public void List_ForTheModelOfAService_HoldsEveryEntityTableExceptTheKeepList(
        string name,
        ServiceName service,
        IModel model,
        string[] keptNames
    )
    {
        var keep = TruncateKeepList.For(service);
        var entityTables = model.GetEntityTypes().Select(entity => $"\"{entity.GetTableName()}\"").ToHashSet();

        var keepTables = model.GetEntityTypes()
            .Where(entity => keep.Contains(entity.ClrType))
            .Select(entity => $"\"{entity.GetTableName()}\"")
            .ToList();

        var tables = TruncateTables.List(model, keep);

        tables.Should().BeEquivalentTo(entityTables.Except(keepTables), name);
        tables.Intersect(keepTables).Should().BeEmpty(name);
        tables.Should().Contain(TruncateTablesTestData.MessageTables.Select(table => $"\"{table}\""), name);
        keepTables.Should().BeEquivalentTo(keptNames.Select(table => $"\"{table}\""), name);
    }

    [Theory(Explicit = true)]
    [MemberData(nameof(TruncateTablesTestData.Contexts), MemberType = typeof(TruncateTablesTestData))]
    public void List_ForTheModelOfAService_LeavesNoKeptTableThatReferencesAnEmptiedTable(
        string name,
        ServiceName service,
        IModel model,
        string[] keptNames
    )
    {
        var keep = TruncateKeepList.For(service);
        var tables = TruncateTables.List(model, keep).ToHashSet();

        var violations = model.GetEntityTypes()
            .Where(entity => keep.Contains(entity.ClrType))
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(key => tables.Contains($"\"{key.PrincipalEntityType.GetTableName()}\""))
            .Select(key => key.DeclaringEntityType.GetTableName())
            .ToList();

        violations.Should().BeEmpty(name);
        tables.Intersect(keptNames.Select(table => $"\"{table}\"")).Should().BeEmpty(name);
    }

    [Fact(Explicit = true)]
    public void BuildSql_WithTables_EmptiesThemInOneStatementWithoutCascade()
    {
        var sql = TruncateTables.BuildSql(["\"a\"", "\"b\""]);

        sql.Should().Be(expected: "TRUNCATE TABLE \"a\", \"b\" RESTART IDENTITY");
    }
}
