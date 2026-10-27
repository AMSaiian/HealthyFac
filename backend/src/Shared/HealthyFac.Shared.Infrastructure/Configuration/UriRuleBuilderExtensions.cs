using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Configuration;

/// <summary>
/// Provides validation rules for URI settings, shared by the options validators of every layer that references
/// Shared.Infrastructure.
/// </summary>
public static class UriRuleBuilderExtensions
{
    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        /// <summary>
        /// Requires the value to be an absolute URI with the http or https scheme.
        /// Pair it with a condition when the setting is optional, because an empty value is not a URI.
        /// </summary>
        /// <returns>The rule builder, so calls can chain.</returns>
        public IRuleBuilderOptions<T, string?> MustBeAbsoluteHttpUri()
            => ruleBuilder
                .Must(value => TryCreateAbsoluteHttpUri(value, out _))
                .WithMessage(ConfigurationValidationMessages.UriMustBeAbsoluteHttp);
    }

    /// <summary>
    /// Parses an absolute URI with the http or https scheme. Validators that check more than the scheme (e.g., an
    /// origin without a path) start from it, so the scheme check exists once.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="uri">The parsed URI when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is an absolute http or https URI.</returns>
    public static bool TryCreateAbsoluteHttpUri(string? value, [NotNullWhen(true)] out Uri? uri)
        => Uri.TryCreate(value, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
