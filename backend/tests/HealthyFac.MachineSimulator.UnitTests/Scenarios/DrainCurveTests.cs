using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

[Feature(TestFeatures.Simulation)]
public sealed class DrainCurveTests
{
    [Theory]
    [MemberData(nameof(DrainCurveTestData.SpindleTemperatureValues), MemberType = typeof(DrainCurveTestData))]
    public void ValueAt_WithFourPointCurve_FollowsThePointsLinearly(string caseName, double progress, double expected)
    {
        var value = DrainCurveTestData.SpindleTemperature.ValueAt(progress);

        value.Should().BeApproximately(expected, precision: 1e-9, caseName);
    }

    [Theory]
    [MemberData(nameof(DrainCurveTestData.TwoPointValues), MemberType = typeof(DrainCurveTestData))]
    public void ValueAt_WithTwoPointCurve_IsLinearOverZeroToOne(string caseName, double progress, double expected)
    {
        var curve = DrainCurve.TwoPoints(start: 31.2, end: 33.4);

        var value = curve.ValueAt(progress);

        value.Should().BeApproximately(expected, precision: 1e-9, caseName);
    }

    [Fact]
    public void OffsetAt_AtAlarm_IsValueMinusReference()
    {
        var offset = DrainCurveTestData.SpindleTemperature.OffsetAt(progress: 0.8);

        offset.Should().BeApproximately(expectedValue: 25, precision: 1e-9);
    }
}
