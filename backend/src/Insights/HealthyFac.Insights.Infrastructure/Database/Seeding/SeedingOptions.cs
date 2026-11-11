using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>
/// Configures which data sets the initializer seeds on startup. Optional data is development and test material.
/// </summary>
public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    /// <summary>Set to true to seed the demo ingest snapshot after the migration (Development only).</summary>
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
