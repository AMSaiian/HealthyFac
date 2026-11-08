using HealthyFac.Telemetry.Infrastructure.Database.Seeding;

namespace HealthyFac.Telemetry.UnitTests.Database.Data;

public static class SeedMachinesValidatorTestData
{
    private const string FirstGatewayId = "seedmachineone22";
    private const string SecondGatewayId = "seedmachinetwo22";

    private static readonly Guid FirstMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    private static readonly Guid SecondMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000102");

    public static TheoryData<IReadOnlyList<SeedMachineOptions>> ValidSeedMachines => new()
    {
        { [Create(FirstMachineId, FirstGatewayId)] },
        {
            [
                Create(FirstMachineId, FirstGatewayId),
                Create(SecondMachineId, SecondGatewayId),
            ]
        },
    };

    public static TheoryData<IReadOnlyList<SeedMachineOptions>, string> InvalidSeedMachines => new()
    {
        {
            [Create(Guid.Empty, FirstGatewayId)],
            SeedingValidationConstants.Messages.SeedMachineIdEmpty
        },
        {
            [Create(FirstMachineId, gatewayId: "tooshort")],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [Create(FirstMachineId, gatewayId: "SEEDMACHINEONE22")],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [Create(FirstMachineId, gatewayId: "seedmachineone18")],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [
                Create(FirstMachineId, FirstGatewayId),
                Create(FirstMachineId, SecondGatewayId),
            ],
            SeedingValidationConstants.Messages.SeedMachineIdDuplicated
        },
        {
            [
                Create(FirstMachineId, FirstGatewayId),
                Create(SecondMachineId, FirstGatewayId),
            ],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdDuplicated
        },
    };

    private static SeedMachineOptions Create(Guid machineId, string gatewayId) =>
        new() { MachineId = machineId, GatewayId = gatewayId };
}
