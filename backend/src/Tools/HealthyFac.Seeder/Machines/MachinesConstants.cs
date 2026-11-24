namespace HealthyFac.Seeder.Machines;

public static class MachinesConstants
{
    public const string FileName = "machines.json";
    public const string MachineSuffix = ".machine";
    public const string AppliedSuffix = ".applied";
    public const string DefinitionsSuffix = ".definitions";
    public const string StateSuffix = ".state";
    public const string SetFileName = "ingest/set-1.json";

    public static readonly TimeSpan DefinitionsDelay = TimeSpan.FromMinutes(minutes: 30);
}
