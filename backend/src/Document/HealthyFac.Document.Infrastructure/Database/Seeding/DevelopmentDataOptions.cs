using FluentValidation;

namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Configures the development data of the document service. The list stays <see langword="null"/> until
/// configuration sets it, because the configuration binder appends configured items to an initialized collection.
/// Read it through <see cref="GetSeedMachines"/>.
/// </summary>
public sealed class DevelopmentDataOptions
{
    public const string SectionName = "DevelopmentData";

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

        RuleForEach(options => options.GetSeedMachines())
            .Must(machine => machine.MachineId != Guid.Empty)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdEmpty)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));

        RuleFor(options => options.GetSeedMachines())
            .Must(machines => machines.Select(machine => machine.MachineId).Distinct().Count() == machines.Count)
            .WithMessage(SeedingValidationConstants.Messages.SeedMachineIdDuplicated)
            .WithName(nameof(DevelopmentDataOptions.SeedMachines));
    }
}
