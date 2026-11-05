namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Defines the exceptions that the state history of a machine throws.
/// </summary>
public static class MachineStateErrors
{
    public static InvalidOperationException StartNotAfterLatest(
        Guid machineId,
        DateTime startedAt,
        DateTime latestStartedAt
    ) =>
        new(
            $"Machine '{machineId}' cannot start a state at '{startedAt:O}', "
            + $"because its latest state started at '{latestStartedAt:O}'."
        );

    public static InvalidOperationException SameState(Guid machineId, MachineState state) =>
        new($"Machine '{machineId}' is already in state '{state.Value}'.");
}
