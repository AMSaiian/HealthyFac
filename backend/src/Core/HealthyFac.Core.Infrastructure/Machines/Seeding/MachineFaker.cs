using Bogus;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.Infrastructure.Machines.Seeding;

/// <summary>
/// Generates development machines of one company. Every machine starts <c>operational</c> and then follows a
/// history that depends on its index in the company, so a company shows every current state.
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
    /// <param name="companyId">The saved company that owns the machines.</param>
    /// <param name="machineTypes">The machine types to pick from. The name appends a two-digit number.</param>
    /// <param name="serialNumberPattern">
    /// The Bogus replacement pattern of serial numbers: <c>#</c> becomes a digit and <c>?</c> becomes a letter.
    /// </param>
    /// <param name="now">
    /// The seed time. The first state starts 30 to 365 days before it, and every change starts before it.
    /// </param>
    /// <param name="seed">The random seed, so every run produces the same machines.</param>
    public MachineFaker(
        Guid companyId,
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
                companyId,
                GatewayIds.Create(),
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
