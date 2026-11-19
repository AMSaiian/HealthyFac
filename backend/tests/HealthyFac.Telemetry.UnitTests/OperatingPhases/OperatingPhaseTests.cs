using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.UnitTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.UnitTests.OperatingPhases;

public sealed class OperatingPhaseTests
{
    [Theory]
    [MemberData(nameof(OperatingPhaseTestData.OperatorsAgainstSources), MemberType = typeof(OperatingPhaseTestData))]
    public void Create_WithOperatorAgainstSource_AcceptsOnlyTheMatchingKind(
        string caseName,
        PhaseConditionOperator conditionOperator,
        PhaseConditionSource source,
        bool isAccepted
    )
    {
        var act = () => OperatingPhaseCondition.Create(
            source,
            conditionOperator,
            value: 1,
            upperValue: 2,
            ["running"]);

        if (isAccepted)
        {
            act.Should().NotThrow(caseName);
        }
        else
        {
            act.Should().Throw<InvalidOperationException>(caseName);
        }
    }

    [Theory]
    [MemberData(nameof(OperatingPhaseTestData.BetweenValuesNotAscending), MemberType = typeof(OperatingPhaseTestData))]
    public void Create_WithBetweenAndValuesNotAscending_Throws(string caseName, double value, double upperValue)
    {
        var act = () => OperatingPhaseCondition.Create(
            OperatingPhaseTestData.NumericSource,
            PhaseConditionOperator.Between,
            value,
            upperValue,
            states: null);

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Theory]
    [MemberData(nameof(OperatingPhaseTestData.InvalidStates), MemberType = typeof(OperatingPhaseTestData))]
    public void Create_WithInAndInvalidStates_Throws(string caseName, IReadOnlyList<string>? states)
    {
        var act = () => OperatingPhaseCondition.Create(
            OperatingPhaseTestData.StateSource,
            PhaseConditionOperator.In,
            value: null,
            upperValue: null,
            states);

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Fact]
    public void Create_WithTwoLimitRowsForOneMetric_Throws()
    {
        var limits = new[]
        {
            OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.OtherNumericMetricId),
            OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.OtherNumericMetricId),
        };

        var act = () => OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], limits);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithLimitRowOnAConditionMetric_Throws()
    {
        var limits = new[] { OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.NumericMetricId) };

        var act = () => OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateNumericCondition()], limits);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReplaceConditions_WithConditionOnAMetricOfALimitRow_Throws()
    {
        var limits = new[] { OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.NumericMetricId) };
        var phase = OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], limits);

        var act = () => phase.ReplaceConditions([OperatingPhaseTestData.CreateNumericCondition()]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(OperatingPhaseTestData.ConditionReplacements), MemberType = typeof(OperatingPhaseTestData))]
    public void ReplaceConditions_WithConditions_ReturnsTrueOnlyForAChange(
        string caseName,
        Func<OperatingPhaseCondition[]> createConditions,
        bool isChanged
    )
    {
        var phase = OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], []);

        var result = phase.ReplaceConditions(createConditions());

        result.Should().Be(isChanged, caseName);
    }

    [Fact]
    public void Update_WithOtherIsTracked_ReturnsTrue()
    {
        var phase = OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], []);

        var result = phase.Update(name: "Cutting", description: null, isTracked: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void Update_WithSameIsTracked_ReturnsFalse()
    {
        var phase = OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], []);

        var result = phase.Update(name: "Cutting 2", description: null, isTracked: true);

        result.Should().BeFalse();
    }

    [Fact]
    public void Replace_WithConditionOnMetricOfOldLimitRowAndNoNewRow_Succeeds()
    {
        var limits = new[] { OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.NumericMetricId) };
        var phase = OperatingPhaseTestData.CreatePhase([OperatingPhaseTestData.CreateStatusCondition()], limits);

        var act = () => phase.Replace([OperatingPhaseTestData.CreateNumericCondition()], []);

        act.Should().NotThrow();
        phase.Limits.Should().BeEmpty();
    }

    [Fact]
    public void Replace_WithLimitRowOfAStoredMetric_ChangesTheStoredRow()
    {
        var phase = OperatingPhaseTestData.CreatePhase(
            [OperatingPhaseTestData.CreateStatusCondition()],
            [OperatingPhaseTestData.CreateLimit(OperatingPhaseTestData.NumericMetricId)]);
        var storedRow = phase.Limits.Single();
        var newRow = OperatingPhaseLimit.Create(
            OperatingPhaseTestData.NumericMetricId,
            MetricValueKind.Numeric,
            new MetricLimits { AlarmHigh = 50 });

        phase.Replace([OperatingPhaseTestData.CreateStatusCondition()], [newRow]);

        phase.Limits.Should().ContainSingle().Which.Should().BeSameAs(storedRow);
        storedRow.AlarmHigh.Should().Be(expected: 50);
    }
}
