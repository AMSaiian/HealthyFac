using FluentValidation;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Configures which data sets the initializer seeds on startup.
/// Required data always seeds. Optional data is development and test material.
/// </summary>
public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    /// <summary>Set to true to seed development and test data after the required data.</summary>
    public bool IsOptionalDataSeeded { get; init; }

    public DevelopmentDataOptions DevelopmentData { get; init; } = new();
}

public sealed class SeedingOptionsValidator : AbstractValidator<SeedingOptions>
{
    public SeedingOptionsValidator()
    {
        RuleFor(options => options.DevelopmentData)
            .NotNull()
            .SetValidator(new DevelopmentDataOptionsValidator());
    }
}
