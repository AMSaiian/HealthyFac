using FluentValidation;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Configures the development data of the telemetry service. The list stays <see langword="null"/> until
/// configuration sets it, because the configuration binder appends configured items to an initialized collection.
/// Read it through <see cref="GetSeedMachines"/>.
/// </summary>
public sealed class DevelopmentDataOptions
{
    public const string SectionName = "DevelopmentData";

    /// <summary>The base seed of the fakers. The same seed produces the same data on every run.</summary>
    public int RandomSeed { get; init; } = 20261106;

    /// <summary>The status definitions of each seed machine. Their codes are unique.</summary>
    public int StatusDefinitionsPerMachine { get; init; } = 4;

    /// <summary>
    /// The company of the test tenant, or <see langword="null"/> to seed nothing.
    /// </summary>
    public TestTenantOptions? TestTenant { get; init; }

    /// <summary>The machines with fixed values, or <see langword="null"/> to seed none.</summary>
    public IReadOnlyList<SeedMachineOptions>? SeedMachines { get; init; }

    public IReadOnlyList<SeedMachineOptions> GetSeedMachines() => SeedMachines ?? [];
}

/// <summary>
/// Validates the development data settings at startup. The seed machine rules equal the rules of Core, so a seed
/// data file that passes in one service passes in the other.
/// </summary>
public sealed class DevelopmentDataOptionsValidator : AbstractValidator<DevelopmentDataOptions>
{
    public DevelopmentDataOptionsValidator()
    {
        RuleFor(options => options.TestTenant!)
            .SetValidator(new TestTenantOptionsValidator())
            .When(options => options.TestTenant is not null);

        RuleFor(options => options.StatusDefinitionsPerMachine)
            .InclusiveBetween(from: 0, SeedingValidationConstants.MaxStatusDefinitionsPerMachine);

        RuleForEach(options => options.GetSeedMachines())
            .Must(machine => machine.MachineId != Guid.Empty)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdEmpty)
            .Must(machine => GatewayIds.IsValid(machine.GatewayId))
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineGatewayIdInvalid)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));

        RuleFor(options => options.GetSeedMachines())
            .Must(machines => machines.Select(machine => machine.MachineId).Distinct().Count() == machines.Count)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdDuplicated)
            .Must(machines => machines.Select(machine => machine.GatewayId).Distinct().Count() == machines.Count)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineGatewayIdDuplicated)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));
    }
}
