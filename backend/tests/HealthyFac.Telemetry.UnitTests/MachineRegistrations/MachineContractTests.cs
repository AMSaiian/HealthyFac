using System.Text.Json;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.UnitTests.MachineRegistrations.Data;
using CoreMachineCreated = HealthyFac.Core.Application.Machines.MachineCreated;
using CoreMachineDeleted = HealthyFac.Core.Application.Machines.MachineDeleted;

namespace HealthyFac.Telemetry.UnitTests.MachineRegistrations;

public sealed class MachineContractTests
{
    [Fact]
    public void MachineCreated_CoreRecord_DeserializesIntoTelemetryRecordWithoutLoss()
    {
        var coreMessage = MachineContractTestData.CreateCoreMachineCreated();
        var json = JsonSerializer.Serialize(coreMessage, MessagingJsonOptions.Instance);

        var telemetryMessage = JsonSerializer.Deserialize<MachineCreated>(json, MessagingJsonOptions.Instance);

        telemetryMessage.Should().BeEquivalentTo(coreMessage);
        MachineCreated.MessageType.Should().Be(CoreMachineCreated.MessageType);
    }

    [Fact]
    public void MachineDeleted_CoreRecord_DeserializesIntoTelemetryRecordWithoutLoss()
    {
        var coreMessage = MachineContractTestData.CreateCoreMachineDeleted();
        var json = JsonSerializer.Serialize(coreMessage, MessagingJsonOptions.Instance);

        var telemetryMessage = JsonSerializer.Deserialize<MachineDeleted>(json, MessagingJsonOptions.Instance);

        telemetryMessage.Should().BeEquivalentTo(coreMessage);
        MachineDeleted.MessageType.Should().Be(CoreMachineDeleted.MessageType);
    }
}
