using HealthyFac.Insights.UnitTests.Database.Data;
using HealthyFac.Shared.Infrastructure.Persistence.Collations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Insights.UnitTests.Database;

public sealed class TextCollationModelTests
{
    private const string JsonbColumnType = "jsonb";

    [Fact]
    public void Model_EveryTextColumn_UsesItsExpectedCollation()
    {
        var wrongProperties = GetTextProperties()
            .Where(property => property.GetCollation() != ExpectedCollation(property))
            .Select(Describe)
            .ToList();

        wrongProperties.Should().BeEmpty();
    }

    [Fact]
    public void Model_CaseSensitiveColumns_ExistInTheModel()
    {
        var caseSensitiveColumns = GetTextProperties()
            .Where(property => property.GetCollation() == CollationNames.CaseSensitive)
            .Select(Describe)
            .ToList();

        caseSensitiveColumns.Should().BeEquivalentTo(TextCollationModelTestData.CaseSensitiveColumns);
    }

    private static List<IProperty> GetTextProperties() =>
        InsightsModelFactory.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(string) && property.GetColumnType() != JsonbColumnType)
            .ToList();

    private static string ExpectedCollation(IProperty property) =>
        TextCollationModelTestData.CaseSensitiveColumns.Contains(Describe(property))
            ? CollationNames.CaseSensitive
            : CollationNames.CaseInsensitive;

    private static string Describe(IProperty property) =>
        $"{property.DeclaringType.DisplayName()}.{property.Name}";
}
