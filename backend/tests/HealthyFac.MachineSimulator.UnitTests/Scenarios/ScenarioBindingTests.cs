using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios;

[Feature(TestFeatures.Simulation)]
public sealed class ScenarioBindingTests
{
    [Theory]
    [MemberData(nameof(ScenarioBindingTestData.Bindings), MemberType = typeof(ScenarioBindingTestData))]
    public void KindOf_WithSerialNumber_BindsTheScenarioIgnoringCase(
        string caseName,
        string serialNumber,
        ScenarioKind expected
    )
    {
        var kind = ScenarioBinding.KindOf(serialNumber);

        kind.Should().Be(expected, caseName);
    }

    [Fact]
    public void Create_ForEachKind_ReturnsAScenarioOfThatKind()
    {
        var kinds = ScenarioKind.All.Select(kind => ScenarioBinding.Create(kind).Kind);

        kinds.Should().Equal(ScenarioKind.All);
    }
}
