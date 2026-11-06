using Bogus;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Database.Seeding;

namespace HealthyFac.Core.Infrastructure.Machines.Seeding;

/// <summary>
/// Generates development machines of one department. The machine takes the company of the department, as the
/// composite department key requires. Every machine starts <c>operational</c> and then follows a history that
/// depends on its index in the department, so a department shows every current state.
/// </summary>
internal sealed class MachineFaker : Faker<Machine>
{
    private static readonly IReadOnlyList<IReadOnlyList<MachineState>> ChangedStatesByCycleIndex =
    [
        [],
        [MachineState.InMaintenance],
        [MachineState.InMaintenance, MachineState.OutOfService],
    ];

    /// <summary>
    /// Declares the machine shape.
    /// </summary>
    /// <param name="department">The saved department that owns the machines.</param>
    /// <param name="machineTypes">The machine types to pick from. The name appends a two-digit number.</param>
    /// <param name="serialNumberPattern">
    /// The Bogus replacement pattern of serial numbers: <c>#</c> becomes a digit and <c>?</c> becomes a letter.
    /// </param>
    /// <param name="now">
    /// The seed time. The first state starts 30 to 365 days before it, and every change starts before it.
    /// </param>
    /// <param name="seed">The random seed, so every run produces the same machines.</param>
    public MachineFaker(
        Department department,
        IReadOnlyList<string> machineTypes,
        string serialNumberPattern,
        DateTime now,
        int seed
    )
    {
        UseSeed(seed);
        CustomInstantiator(faker =>
        {
            var firstStartedAt = now
                - TimeSpan.FromDays(
                    faker.Random.Double(
                        SeedingValidationConstants.StateHistory.MinStartDaysBeforeNow,
                        SeedingValidationConstants.StateHistory.MaxStartDaysBeforeNow));
            var machine = new Machine(
                department.CompanyId,
                department.Id,
                $"{faker.PickRandom<string>(machineTypes)} {faker.IndexFaker + 1:00}",
                faker.Random.Replace(serialNumberPattern),
                description: null,
                firstStartedAt);

            var changedStates =
                ChangedStatesByCycleIndex[faker.IndexFaker % ChangedStatesByCycleIndex.Count];
            var previousStartedAt = firstStartedAt;

            foreach (var state in changedStates)
            {
                previousStartedAt += (now - previousStartedAt)
                    * faker.Random.Double(
                        SeedingValidationConstants.StateHistory.MinChangeShare,
                        SeedingValidationConstants.StateHistory.MaxChangeShare);
                machine.ChangeState(state, previousStartedAt);
            }

            return machine;
        });
    }
}
