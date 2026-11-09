using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;

public static class MachineRegistrationTestData
{
    public const int RaceParticipantCount = 2;

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a02");

    public static MachineCreated CreateMessage() =>
        new()
        {
            MachineId = Guid.CreateVersion7(),
            GatewayId = GatewayIds.Create(),
            CompanyId = CompanyId,
        };

    public static MachineDeleted CreateDeletedMessage(Guid machineId) =>
        new() { MachineId = machineId, CompanyId = CompanyId };
}
