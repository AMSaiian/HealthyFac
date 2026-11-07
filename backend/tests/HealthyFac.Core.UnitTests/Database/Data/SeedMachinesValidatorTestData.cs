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
        { [Create(FirstMachineId, FirstGatewayId, departmentIndex: 0)] },
        { [Create(FirstMachineId, FirstGatewayId, departmentIndex: 1)] },
        {
            [
                Create(FirstMachineId, FirstGatewayId, departmentIndex: 0),
                Create(SecondMachineId, SecondGatewayId, departmentIndex: 1),
            ]
        },
    };

    public static TheoryData<IReadOnlyList<SeedMachineOptions>, string> InvalidSeedMachines => new()
    {
        {
            [Create(Guid.Empty, FirstGatewayId, departmentIndex: 0)],
            SeedingValidationConstants.Messages.SeedMachineIdEmpty
        },
        {
            [Create(FirstMachineId, gatewayId: "tooshort", departmentIndex: 0)],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [Create(FirstMachineId, gatewayId: "SEEDMACHINEONE22", departmentIndex: 0)],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [Create(FirstMachineId, gatewayId: "seedmachineone18", departmentIndex: 0)],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid
        },
        {
            [Create(FirstMachineId, FirstGatewayId, departmentIndex: -1)],
            SeedingValidationConstants.Messages.SeedMachineDepartmentIndexOutOfRange
        },
        {
            [Create(FirstMachineId, FirstGatewayId, departmentIndex: 2)],
            SeedingValidationConstants.Messages.SeedMachineDepartmentIndexOutOfRange
        },
        {
            [
                Create(FirstMachineId, FirstGatewayId, departmentIndex: 0),
                Create(FirstMachineId, SecondGatewayId, departmentIndex: 0),
            ],
            SeedingValidationConstants.Messages.SeedMachineIdDuplicated
        },
        {
            [
                Create(FirstMachineId, FirstGatewayId, departmentIndex: 0),
                Create(SecondMachineId, FirstGatewayId, departmentIndex: 0),
            ],
            SeedingValidationConstants.Messages.SeedMachineGatewayIdDuplicated
        },
    };

    public static SeedMachineOptions SingleSeedMachine =>
        Create(FirstMachineId, FirstGatewayId, departmentIndex: 0);

    private static SeedMachineOptions Create(Guid machineId, string gatewayId, int departmentIndex) =>
        new() { MachineId = machineId, GatewayId = gatewayId, DepartmentIndex = departmentIndex };
}
