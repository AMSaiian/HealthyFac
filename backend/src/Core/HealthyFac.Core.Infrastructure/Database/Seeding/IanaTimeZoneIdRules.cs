using FluentValidation;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the shared time zone rule of the development data validators.
/// </summary>
public static class IanaTimeZoneIdRules
{
    /// <summary>
    /// Requires an IANA time zone ID that the host knows.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="ruleBuilder">The rule builder of a text member.</param>
    /// <returns>The rule builder, to chain more rules.</returns>
    public static IRuleBuilderOptions<T, string> IanaTimeZoneId<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Must(timeZoneId => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            .WithMessage(SeedingValidationConstants.Messages.TimeZoneIdUnknown);
}
