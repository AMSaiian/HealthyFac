using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>Configures the development data of the insights service.</summary>
public sealed class DevelopmentDataOptions
{
    public const string SectionName = "DevelopmentData";

    /// <summary>The test tenant, or <see langword="null"/> to seed nothing.</summary>
    public TestTenantOptions? TestTenant { get; init; }
}

public sealed class DevelopmentDataOptionsValidator : AbstractValidator<DevelopmentDataOptions>
{
    public DevelopmentDataOptionsValidator()
    {
        RuleFor(options => options.TestTenant!)
            .SetValidator(new TestTenantOptionsValidator())
            .When(options => options.TestTenant is not null);
    }
}
