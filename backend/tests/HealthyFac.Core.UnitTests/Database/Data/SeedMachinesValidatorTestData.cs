using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.UnitTests.Database.Data;

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
        { [Create(FirstMachineId, FirstGatewayId, name: "Seed machine 01")] },
        { [Create(FirstMachineId, FirstGatewayId, new string(c: 'n', MachineValidationConstants.NameMaxLength))] },
    };

    public static TheoryData<IReadOnlyList<SeedMachineOptions>, string> InvalidSeedMachines => new()
    {
        {
            [Create(Guid.Empty, FirstGatewayId)],
            SeedingValidationConstants.Messages.SeedMachineIdEmpty
        },
        {
            [Create(FirstMachineId, FirstGatewayId, name: "   ")],
            SeedingValidationConstants.Messages.SeedMachineNameInvalid
        },
        {
            [Create(FirstMachineId, FirstGatewayId, new string(c: 'n', MachineValidationConstants.NameMaxLength + 1))],
            SeedingValidationConstants.Messages.SeedMachineNameInvalid
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

    private static SeedMachineOptions Create(Guid machineId, string gatewayId, string? name = null) =>
        new() { MachineId = machineId, GatewayId = gatewayId, Name = name };
}
