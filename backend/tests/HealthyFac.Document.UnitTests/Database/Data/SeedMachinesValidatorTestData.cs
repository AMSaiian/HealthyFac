using HealthyFac.Document.Infrastructure.Database.Seeding;

namespace HealthyFac.Document.UnitTests.Database.Data;

public static class SeedMachinesValidatorTestData
{
    private static readonly Guid FirstMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    private static readonly Guid SecondMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000102");

    public static TheoryData<IReadOnlyList<SeedMachineOptions>> ValidSeedMachines => new()
    {
        { [Create(FirstMachineId)] },
        { [Create(FirstMachineId), Create(SecondMachineId)] },
    };

    public static TheoryData<IReadOnlyList<SeedMachineOptions>, string> InvalidSeedMachines => new()
    {
        {
            [Create(Guid.Empty)],
            SeedingValidationConstants.Messages.SeedMachineIdEmpty
        },
        {
            [Create(FirstMachineId), Create(FirstMachineId)],
            SeedingValidationConstants.Messages.SeedMachineIdDuplicated
        },
    };

    private static SeedMachineOptions Create(Guid machineId) => new() { MachineId = machineId };
}
