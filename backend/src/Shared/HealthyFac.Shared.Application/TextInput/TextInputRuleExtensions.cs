using FluentValidation;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Adds the text input rules to a string rule. Every string of a request or a message gets one of them in its
/// validator (see <c>ConventionChecks.FindStringsWithoutTextRule</c>).
/// </summary>
public static class TextInputRuleExtensions
{
    extension<T>(IRuleBuilder<T, string?> rule)
    {
        /// <summary>
        /// Rejects the null character and every control character.
        /// </summary>
        /// <returns>The rule builder, for chaining.</returns>
        public IRuleBuilderOptions<T, string?> SingleLineText() =>
            rule.SetValidator(new TextInputPropertyValidator<T>(TextInputKind.SingleLine));

        /// <summary>
        /// Rejects the null character and every control character except <c>\n</c> and <c>\t</c>.
        /// </summary>
        /// <returns>The rule builder, for chaining.</returns>
        public IRuleBuilderOptions<T, string?> MultilineText() =>
            rule.SetValidator(new TextInputPropertyValidator<T>(TextInputKind.Multiline));
    }
}
