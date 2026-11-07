using CoreMachineCreated = HealthyFac.Core.Application.Machines.MachineCreated;
using CoreMachineDeleted = HealthyFac.Core.Application.Machines.MachineDeleted;

namespace HealthyFac.Telemetry.UnitTests.MachineRegistrations.Data;

public static class MachineContractTestData
{
    public static CoreMachineCreated CreateCoreMachineCreated() =>
        new()
        {
            MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101"),
            GatewayId = "seedmachineone22",
            CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01"),
            DepartmentId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01"),
        };

    public static CoreMachineDeleted CreateCoreMachineDeleted() =>
        new()
        {
            MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101"),
            CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01"),
        };
}
