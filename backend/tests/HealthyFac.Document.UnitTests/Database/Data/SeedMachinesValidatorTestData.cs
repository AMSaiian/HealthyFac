using HealthyFac.Core.Messaging.Machines;
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
        { [Create(FirstMachineId, name: "Seed machine 01")] },
        { [Create(FirstMachineId, new string(c: 'n', MachineMessageLimits.NameMaxLength))] },
    };

    public static TheoryData<IReadOnlyList<SeedMachineOptions>, string> InvalidSeedMachines => new()
    {
        {
            [Create(Guid.Empty)],
            SeedingValidationConstants.Messages.SeedMachineIdEmpty
        },
        {
            [Create(FirstMachineId, name: "   ")],
            SeedingValidationConstants.Messages.SeedMachineNameInvalid
        },
        {
            [Create(FirstMachineId, new string(c: 'n', MachineMessageLimits.NameMaxLength + 1))],
            SeedingValidationConstants.Messages.SeedMachineNameInvalid
        },
        {
            [Create(FirstMachineId), Create(FirstMachineId)],
            SeedingValidationConstants.Messages.SeedMachineIdDuplicated
        },
    };

    private static SeedMachineOptions Create(Guid machineId, string? name = null) =>
        new() { MachineId = machineId, Name = name };
}
