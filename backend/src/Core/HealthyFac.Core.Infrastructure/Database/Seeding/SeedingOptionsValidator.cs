using FluentValidation;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.Infrastructure.Database;

public sealed class SeedingOptionsValidator : AbstractValidator<SeedingOptions>
{
    public SeedingOptionsValidator(IHostEnvironment environment)
    {
        RuleFor(options => options.DevelopmentData)
            .NotNull()
            .SetValidator(new DevelopmentDataOptionsValidator(environment));
    }
}
