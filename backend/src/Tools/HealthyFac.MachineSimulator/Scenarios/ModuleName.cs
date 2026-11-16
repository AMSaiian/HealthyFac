using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The module names of all detailed scenarios. The camelCase values reach the control API. A scenario holds a subset.
/// </summary>
public sealed class ModuleName : Enumeration<ModuleName>
{
    public static readonly ModuleName Spindle = new(value: "spindle");
    public static readonly ModuleName Hydraulics = new(value: "hydraulics");
    public static readonly ModuleName Coolant = new(value: "coolant");
    public static readonly ModuleName Lubrication = new(value: "lubrication");
    public static readonly ModuleName Cabinet = new(value: "cabinet");
    public static readonly ModuleName Bearings = new(value: "bearings");
    public static readonly ModuleName Seal = new(value: "seal");
    public static readonly ModuleName Suction = new(value: "suction");
    public static readonly ModuleName Discharge = new(value: "discharge");
    public static readonly ModuleName Motor = new(value: "motor");
    public static readonly ModuleName OilCooler = new(value: "oilCooler");
    public static readonly ModuleName OilSeparator = new(value: "oilSeparator");
    public static readonly ModuleName OilFilter = new(value: "oilFilter");
    public static readonly ModuleName AirFilter = new(value: "airFilter");
    public static readonly ModuleName Converter = new(value: "converter");
    public static readonly ModuleName CondensateDrain = new(value: "condensateDrain");

    private ModuleName(string value)
        : base(value)
    {
    }
}
