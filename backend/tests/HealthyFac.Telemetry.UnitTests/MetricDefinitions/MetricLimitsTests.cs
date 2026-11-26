using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.UnitTests.MetricDefinitions.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.MetricDefinitions;

[Feature(TestFeatures.SignalModel)]
public sealed class MetricLimitsTests
{
    [Theory]
    [MemberData(nameof(MetricLimitsTestData.InvalidLimits), MemberType = typeof(MetricLimitsTestData))]
    public void EnsureValid_WithLimitsThatBreakARule_Throws(string caseName, MetricValueKind kind, MetricLimits limits)
    {
        var act = () => limits.EnsureValid(kind);

        act.Should().Throw<InvalidOperationException>(caseName);
    }

    [Theory]
    [MemberData(nameof(MetricLimitsTestData.ValidLimits), MemberType = typeof(MetricLimitsTestData))]
    public void EnsureValid_WithLimitsThatFollowTheRules_DoesNotThrow(
        string caseName,
        MetricValueKind kind,
        MetricLimits limits
    )
    {
        var act = () => limits.EnsureValid(kind);

        act.Should().NotThrow(caseName);
    }

    [Fact]
    public void ChangeLimits_WithInvalidLimits_KeepsTheStoredLimits()
    {
        var metric = MetricDefinitionTestData.CreateBoolean();

        var act = () => metric.ChangeLimits(new MetricLimits { AlarmHigh = 1 }, MetricBehavior.Level);

        act.Should().Throw<InvalidOperationException>();
        metric.Limits.Should().Be(MetricLimits.None);
    }

    [Fact]
    public void ChangeLimits_WithValidLimits_StoresLimitsAndBehavior()
    {
        var metric = MetricDefinitionTestData.CreateNumeric();
        var limits = new MetricLimits { WarningHigh = 80, AlarmHigh = 90 };

        metric.ChangeLimits(limits, MetricBehavior.Counter);

        metric.Limits.Should().Be(limits);
        metric.Behavior.Should().Be(MetricBehavior.Counter);
    }
}
