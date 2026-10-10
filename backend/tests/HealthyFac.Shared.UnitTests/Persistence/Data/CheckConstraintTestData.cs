namespace HealthyFac.Shared.UnitTests.Persistence.Data;

public static class CheckConstraintTestData
{
    public static TheoryData<string, double, double, string> RangeExpressions => new()
    {
        { "ratio", 0.5, 1.5, @"""ratio"" BETWEEN 0.5 AND 1.5" },
    };

    public static TheoryData<string, string[], string> AllowedValueExpressions => new()
    {
        { "state", ["operational", "inMaintenance"], @"""state"" IN ('inMaintenance', 'operational')" },
        { "code", ["it's"], @"""code"" IN ('it''s')" },
    };

    public const string LongEntity = "an_entity_with_a_very_long_name_that_overflows";
}
