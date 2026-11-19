using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;

namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

public static class ModuleStateTestData
{
    public static readonly TimeSpan DrainDuration = TimeSpan.FromMinutes(minutes: 3);

    /// <summary>A start mode, a change, the expected mode, and the expected progress.</summary>
    public static TheoryData<string, ModuleMode, ModuleMode, ModuleMode, double> ModeChanges =>
        new()
        {
            { "DrainingFromExpected", ModuleMode.Expected, ModuleMode.Draining, ModuleMode.Draining, 0 },
            { "FailureFromExpected", ModuleMode.Expected, ModuleMode.Failure, ModuleMode.Failure, 1 },
            { "FailureFromDraining", ModuleMode.Draining, ModuleMode.Failure, ModuleMode.Failure, 1 },
            { "ExpectedFromDraining", ModuleMode.Draining, ModuleMode.Expected, ModuleMode.Expected, 0 },
            { "ExpectedFromFailure", ModuleMode.Failure, ModuleMode.Expected, ModuleMode.Expected, 0 },
            { "DrainingFromFailure", ModuleMode.Failure, ModuleMode.Draining, ModuleMode.Draining, 0 },
            { "FailureRepeat", ModuleMode.Failure, ModuleMode.Failure, ModuleMode.Failure, 1 },
        };

    /// <summary>A time to advance after a start at progress 0 and the milestones that it crosses.</summary>
    public static TheoryData<string, int, DrainMilestone[]> Advances =>
        new()
        {
            { "BeforeFirstMilestone", 80, [] },
            { "AtNormalLimit", 90, [DrainMilestone.NormalLimit] },
            { "AtAlarm", 144, [DrainMilestone.NormalLimit, DrainMilestone.Alarm] },
            { "AtTrip", 180, [DrainMilestone.NormalLimit, DrainMilestone.Alarm, DrainMilestone.Trip] },
        };
}
