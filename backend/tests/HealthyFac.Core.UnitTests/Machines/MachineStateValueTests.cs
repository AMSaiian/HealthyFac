using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.UnitTests.Machines;

public sealed class MachineStateValueTests
{
    [Fact]
    public void All_ListsExactlyTheValuesOfMachineState()
    {
        var messageValues = MachineStateValue.All.Select(state => state.Value);
        var domainValues = MachineState.All.Select(state => state.Value);

        messageValues.Should().BeEquivalentTo(domainValues);
    }
}
