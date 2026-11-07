using FluentValidation;
using HealthyFac.Core.Application.Departments;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Shared.Domain.Gateways;
using Microsoft.Extensions.Hosting;

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

    /// <summary>
    /// The test company and its first supervisor, or <see langword="null"/> to seed none. The supervisor is active
    /// and linked to the Keycloak user of the same realm import.
    /// </summary>
    public TestTenantOptions? TestTenant { get; init; }

    /// <summary>
    /// The machines with fixed IDs, or <see langword="null"/> to seed none. They belong to the test tenant, so
    /// Core seeds them only together with it.
    /// </summary>
    public IReadOnlyList<SeedMachineOptions>? SeedMachines { get; init; }

    public IReadOnlyList<string>? DepartmentNames { get; init; }

    /// <summary>The IANA time zone IDs that departments take.</summary>
    public IReadOnlyList<string>? TimeZoneIds { get; init; }

    public IReadOnlyList<string>? MachineTypes { get; init; }

    public IReadOnlyList<SeedMachineOptions> GetSeedMachines() => SeedMachines ?? [];

    public IReadOnlyList<string> GetDepartmentNames() => DepartmentNames ?? DefaultDepartmentNames;

    public IReadOnlyList<string> GetTimeZoneIds() => TimeZoneIds ?? DefaultTimeZoneIds;

    public IReadOnlyList<string> GetMachineTypes() => MachineTypes ?? DefaultMachineTypes;
}

/// <summary>
/// Validates the development data settings at startup. It checks the effective lists, so a configured list and a
/// default list get the same checks, and every generated value fits the database constraints. It rejects forced
/// recreation outside the Development environment, so a wrong setting cannot delete data in a shared environment.
/// </summary>
public sealed class DevelopmentDataOptionsValidator : AbstractValidator<DevelopmentDataOptions>
{
    /// <summary>
    /// Declares the rules.
    /// </summary>
    /// <param name="environment">The host environment that decides whether forced recreation is allowed.</param>
    public DevelopmentDataOptionsValidator(IHostEnvironment environment)
    {
        RuleFor(options => options.IsRecreationForced)
            .Equal(toCompare: false)
            .Unless(_ => environment.IsDevelopment())
            .WithMessage(SeedingValidationConstants.Messages.RecreationOnlyInDevelopment);

        RuleFor(options => options.TestTenant!)
            .SetValidator(new TestTenantOptionsValidator())
            .When(options => options.TestTenant is not null);

        RuleForEach(options => options.GetSeedMachines())
            .Must(machine => machine.MachineId != Guid.Empty)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdEmpty)
            .Must(machine => GatewayIds.IsValid(machine.GatewayId))
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid)
            .Must((options, machine) => machine.DepartmentIndex >= 0
                && machine.DepartmentIndex < (options.TestTenant?.DepartmentIds.Count ?? 0))
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineDepartmentIndexOutOfRange)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));

        RuleFor(options => options.GetSeedMachines())
            .Must(machines => machines.Select(machine => machine.MachineId).Distinct().Count() == machines.Count)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdDuplicated)
            .Must(machines => machines.Select(machine => machine.GatewayId).Distinct().Count() == machines.Count)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineGatewayIdDuplicated)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));

        RuleFor(options => options.CompanyCount)
            .InclusiveBetween(from: 1, SeedingValidationConstants.Counts.MaxCompanies);

        RuleFor(options => options.DepartmentsPerCompany)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .LessThanOrEqualTo(options => options.GetDepartmentNames().Count);

        RuleFor(options => options.MachinesPerDepartment)
            .InclusiveBetween(from: 0, SeedingValidationConstants.Counts.MaxPerDepartment);

        RuleFor(options => options.SupervisorsPerDepartment)
            .InclusiveBetween(from: 0, SeedingValidationConstants.Counts.MaxSupervisorsPerDepartment);

        RuleFor(options => options.WorkersPerDepartment)
            .InclusiveBetween(from: 0, SeedingValidationConstants.Counts.MaxPerDepartment);

        RuleFor(options => options.OpenInvitesPerCompany)
            .InclusiveBetween(from: 0, SeedingValidationConstants.Counts.MaxOpenInvitesPerCompany);

        RuleFor(options => options.InviteLifetimeDays)
            .InclusiveBetween(
                SeedingValidationConstants.InviteLifetime.MinDays,
                SeedingValidationConstants.InviteLifetime.MaxDays);

        RuleFor(options => options.SerialNumberPattern)
            .NotEmpty()
            .MaximumLength(MachineValidationConstants.SerialNumberMaxLength);

        RuleFor(options => options.GetDepartmentNames())
            .NotEmpty()
            .Must(names => names.Distinct(StringComparer.Ordinal).Count() == names.Count)
            .WithName(nameof(DevelopmentDataOptions.DepartmentNames));

        RuleForEach(options => options.GetDepartmentNames())
            .NotEmpty()
            .MaximumLength(DepartmentValidationConstants.NameMaxLength)
            .WithName(nameof(DevelopmentDataOptions.DepartmentNames));

        RuleFor(options => options.GetTimeZoneIds())
            .NotEmpty()
            .WithName(nameof(DevelopmentDataOptions.TimeZoneIds));

        RuleForEach(options => options.GetTimeZoneIds())
            .NotEmpty()
            .MaximumLength(DepartmentValidationConstants.TimeZoneIdMaxLength)
            .Must(timeZoneId => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            .WithName(nameof(DevelopmentDataOptions.TimeZoneIds));

        RuleFor(options => options.GetMachineTypes())
            .NotEmpty()
            .WithName(nameof(DevelopmentDataOptions.MachineTypes));

        RuleForEach(options => options.GetMachineTypes())
            .NotEmpty()
            .MaximumLength(
                MachineValidationConstants.NameMaxLength - SeedingValidationConstants.MachineNameSuffixLength)
            .WithName(nameof(DevelopmentDataOptions.MachineTypes));
    }
}
