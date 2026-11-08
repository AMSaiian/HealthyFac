using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.UnitTests.MetricDefinitions.Data;

namespace HealthyFac.Telemetry.UnitTests.MetricDefinitions;

public sealed class MetricDefinitionTests
{
    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.ValidKeys), MemberType = typeof(MetricDefinitionTestData))]
    public void IsValidKey_WithValidKey_ReturnsTrue(string key)
    {
        var isValid = MetricDefinitionRules.IsValidKey(key);

        isValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.InvalidKeys), MemberType = typeof(MetricDefinitionTestData))]
    public void IsValidKey_WithInvalidKey_ReturnsFalse(string key)
    {
        var isValid = MetricDefinitionRules.IsValidKey(key);

        isValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.AscendingRanges), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithValidRange_StoresRange(double? min, double? max)
    {
        var metric = MetricDefinitionTestData.CreateNumeric(min, max);

        metric.Min.Should().Be(min);
        metric.Max.Should().Be(max);
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.NotAscendingRanges), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithMinNotBelowMax_Throws(double min, double max)
    {
        var act = () => MetricDefinitionTestData.CreateNumeric(min, max);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.InvalidKeys), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithInvalidKey_Throws(string key)
    {
        var act = () => MetricDefinitionTestData.CreateWithKey(key);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.InvalidCombinations), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithFieldsThatBreakTheKind_Throws(
        MetricValueKind kind,
        MeasurementUnit unit,
        double? min,
        double? max,
        string[]? states
    )
    {
        var act = () => MetricDefinitionTestData.CreateWithFields(kind, unit, min, max, states);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(
        nameof(MetricDefinitionTestData.DiscreteKindsWithWrongUnit), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithDiscreteKindAndWrongUnit_Throws(MetricValueKind kind, MeasurementUnit unit)
    {
        var act = () => MetricDefinitionTestData.CreateWithFields(kind, unit, min: null, max: null, states: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.DiscreteUnitList), MemberType = typeof(MetricDefinitionTestData))]
    public void Constructor_WithNumericAndDiscreteUnit_Throws(MeasurementUnit unit)
    {
        var act = () => MetricDefinitionTestData.CreateWithFields(
            MetricValueKind.Numeric, unit, min: null, max: null, states: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_WithNewValues_ChangesOnlyEditableFields()
    {
        var metric = MetricDefinitionTestData.CreateNumeric(min: 0, max: 100);

        metric.Update(name: "New name", description: "<p>text</p>", min: 5, max: 50, states: null);

        metric.Name.Should().Be(expected: "New name");
        metric.Description.Should().Be(expected: "<p>text</p>");
        metric.Min.Should().Be(expected: 5);
        metric.Max.Should().Be(expected: 50);
        metric.Key.Should().Be(expected: "spindle.temperature");
        metric.MeasurementUnitId.Should().Be(MetricDefinitionTestData.CelsiusUnit.Id);
    }

    [Fact]
    public void Update_WithRangeOfEqualBounds_Throws()
    {
        var metric = MetricDefinitionTestData.CreateNumeric(min: 0, max: 100);

        var act = () => metric.Update(name: "Name", description: null, min: 10, max: 10, states: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_OfStateSetWithOneState_Throws()
    {
        var metric = MetricDefinitionTestData.CreateStateSet();

        var act = () => metric.Update(name: "Name", description: null, min: null, max: null, ["auto"]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_OfBooleanWithStates_Throws()
    {
        var metric = MetricDefinitionTestData.CreateBoolean();

        var act = () => metric.Update(name: "Name", description: null, min: null, max: null, ["a", "b"]);

        act.Should().Throw<InvalidOperationException>();
    }
}
