using FluentValidation;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Configuration;

/// <summary>
/// Validates strongly-typed configuration options using FluentValidation.
/// </summary>
public sealed class ConfigurationValidator<TOptions>(
    string? optionsName,
    IValidator<TOptions> validator
) : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (name is not null && name != optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        ArgumentNullException.ThrowIfNull(options);

        var validationResult = validator.Validate(options);
        if (validationResult.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = validationResult.Errors
            .Select(error => ConfigurationValidationConstants.FormatFailure(
                    typeof(TOptions).Name,
                    error.PropertyName,
                    error.ErrorMessage
                )
            );

        return ValidateOptionsResult.Fail(errors);
    }
}
