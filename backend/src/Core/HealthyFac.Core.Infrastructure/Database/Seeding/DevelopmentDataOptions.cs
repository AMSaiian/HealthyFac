namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Configures the amount and the shape of the generated development data. Every value has a default, so the
/// section is optional. The list settings stay <see langword="null"/> until configuration sets them, because the
/// configuration binder appends configured items to an initialized collection instead of replacing it. Read the
/// lists through the <c>Get…</c> methods, which fall back to the defaults.
/// </summary>
public sealed class DevelopmentDataOptions
{
    public const string SectionName = "DevelopmentData";

    private static readonly IReadOnlyList<string> DefaultDepartmentNames =
        ["Assembly", "Machining", "Packaging", "Welding", "Paint shop", "Quality control"];

    private static readonly IReadOnlyList<string> DefaultTimeZoneIds =
        ["Europe/Kyiv", "Europe/Berlin", "America/Chicago"];

    private static readonly IReadOnlyList<string> DefaultMachineTypes =
        ["CNC lathe", "Milling machine", "Hydraulic press", "Laser cutter", "Injection molder", "Conveyor"];

    /// <summary>
    /// Set to true to delete all companies, departments, machines, employees, and invites on start and generate
    /// them again. Roles stay, because they are required data. Validation allows it only in the Development
    /// environment. Every start recreates the data while it is true, so turn it off after use.
    /// </summary>
    public bool IsRecreationForced { get; init; }

    /// <summary>The base seed of all fakers. The same seed produces the same data on every run.</summary>
    public int RandomSeed { get; init; } = 20261021;

    public int CompanyCount { get; init; } = 2;

    /// <summary>The departments per company. Each takes the next name, so it cannot exceed the name count.</summary>
    public int DepartmentsPerCompany { get; init; } = 3;

    public int MachinesPerDepartment { get; init; } = 4;

    public int SupervisorsPerDepartment { get; init; } = 1;

    public int WorkersPerDepartment { get; init; } = 3;

    public int OpenInvitesPerCompany { get; init; } = 2;

    public int InviteLifetimeDays { get; init; } = 7;

    /// <summary>
    /// The Bogus replacement pattern of machine serial numbers: <c>#</c> becomes a digit and <c>?</c> becomes a
    /// letter.
    /// </summary>
    public string SerialNumberPattern { get; init; } = "SN-####-????";

    public IReadOnlyList<string>? DepartmentNames { get; init; }

    /// <summary>The IANA time zone IDs that departments take.</summary>
    public IReadOnlyList<string>? TimeZoneIds { get; init; }

    public IReadOnlyList<string>? MachineTypes { get; init; }

    public IReadOnlyList<string> GetDepartmentNames() => DepartmentNames ?? DefaultDepartmentNames;

    public IReadOnlyList<string> GetTimeZoneIds() => TimeZoneIds ?? DefaultTimeZoneIds;

    public IReadOnlyList<string> GetMachineTypes() => MachineTypes ?? DefaultMachineTypes;
}
