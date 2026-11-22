namespace HealthyFac.Seeder.Machines;

/// <summary>Defines the error messages of the machines part.</summary>
public static class MachinesErrors
{
    public static string DraftUnknown(string item, Guid draftId) =>
        $"The event {item} names the machine draft {draftId}, which ingest/set-1.json does not hold.";

    public static string DraftIncomplete(string item, string member) =>
        $"The event {item} applies a draft without a value for {member}.";

    public static string AppliedNeedsTime(string item) => $"The event {item} needs a moment with a time of day.";

    public static string AppliedTooLate(string item) =>
        $"The event {item} lies too late in the day: the definitions 30 minutes later would pass midnight.";

    public static string MachineUnknown(string key) => $"The seed has no machine with the key {key}.";

    public static string RegistrationMissing(Guid machineId) =>
        $"Telemetry holds no registration of the machine {machineId}.";

    public static string DefinitionMissing(string key) => $"Telemetry holds no definition {key} of the machine.";
}
