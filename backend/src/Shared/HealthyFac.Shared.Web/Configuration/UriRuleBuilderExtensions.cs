using FluentValidation;

namespace HealthyFac.Shared.Web.Configuration;

/// <summary>
/// Provides validation rules for URI settings, shared by the options validators.
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
                .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage(ConfigurationValidationMessages.UriMustBeAbsoluteHttp);
    }
}
