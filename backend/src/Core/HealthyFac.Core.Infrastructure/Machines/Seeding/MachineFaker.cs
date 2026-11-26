using Bogus;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.Infrastructure.Machines.Seeding;

/// <summary>
/// Generates development machines of one company. Every machine starts <c>operational</c> and then follows a
/// history that depends on its index in the company, so a company shows every current state.
/// </summary>
internal sealed class MachineFaker : Faker<Machine>
{
    private const int MinStartDaysBeforeNow = 30;
    private const int MaxStartDaysBeforeNow = 365;
    private const double MinChangeShare = 0.1;
    private const double MaxChangeShare = 0.9;
    private const string SerialNumberLetterPattern = "????";
    private const string SerialNumberFormat = "SN-{0:0000}-{1}";

    private static readonly IReadOnlyList<string> MachineTypes =
        ["CNC lathe", "Milling machine", "Hydraulic press", "Laser cutter", "Injection molder", "Conveyor"];

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
    /// <param name="timeZoneId">
    /// The IANA time zone of the company. The expected go-live day is the day of the first state start in this
    /// zone, so the state history never starts before it. UTC applies when the company has none.
    /// </param>
    /// <param name="now">
    /// The seed time. The first state starts 30 to 365 days before it, and every change starts before it.
    /// </param>
    /// <param name="seed">The random seed, so every run produces the same machines.</param>
    public MachineFaker(
        Guid companyId,
        string? timeZoneId,
        DateTime now,
        int seed
    )
    {
        UseSeed(seed);
        var zone = timeZoneId is null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        CustomInstantiator(faker =>
        {
            var firstStartedAt = now
                - TimeSpan.FromDays(
                    faker.Random.Double(
                        MinStartDaysBeforeNow,
                        MaxStartDaysBeforeNow));

            var machine = new Machine(
                companyId,
                GatewayIds.Create(),
                $"{faker.PickRandom<string>(MachineTypes)} {faker.IndexFaker + 1:00}",
                string.Format(
                    SerialNumberFormat,
                    faker.IndexFaker + 1,
                    faker.Random.Replace(SerialNumberLetterPattern).ToUpperInvariant()),
                description: null,
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(firstStartedAt, zone)),
                firstStartedAt);

            var changedStates =
                ChangedStatesByCycleIndex[faker.IndexFaker % ChangedStatesByCycleIndex.Count];

            var previousStartedAt = firstStartedAt;

            foreach (var state in changedStates)
            {
                previousStartedAt += (now - previousStartedAt)
                    * faker.Random.Double(
                        MinChangeShare,
                        MaxChangeShare);

                machine.ChangeState(state, previousStartedAt);
            }

            return machine;
        });
    }
}
