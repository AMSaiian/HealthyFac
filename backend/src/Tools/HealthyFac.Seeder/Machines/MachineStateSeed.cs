using HealthyFac.Core.Domain.Machines;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Machines;

public sealed class MachineStateSeed
{
    public required string Machine { get; init; }

    public required string ChangedBy { get; init; }

    public required RelativeMoment At { get; init; }

    public required MachineState State { get; init; }
}
