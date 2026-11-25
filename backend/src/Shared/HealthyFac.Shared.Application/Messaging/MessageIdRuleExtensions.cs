using FluentValidation;

namespace HealthyFac.Shared.Application.Messaging;

/// <summary>
/// Holds the ID rule of the message validators.
/// </summary>
public static class MessageIdRuleExtensions
{
    extension<T>(IRuleBuilder<T, Guid?> rule)
    {
        /// <summary>
        /// Requires a set ID: not <see langword="null"/> and not <see cref="Guid.Empty"/>. The built-in <c>NotEmpty</c>
        /// compares a <c>Guid?</c> with <see langword="null"/> only, so it lets <see cref="Guid.Empty"/> pass.
        /// </summary>
        /// <returns>The rule builder, for the message and the condition.</returns>
        public IRuleBuilderOptions<T, Guid?> MustBeSetId() =>
            rule.Must(id => id is { } value && value != Guid.Empty);
    }
}
