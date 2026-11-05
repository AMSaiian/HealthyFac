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
    private readonly string? _optionsName = optionsName;
    private readonly IValidator<TOptions> _validator = validator;

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (name is not null && name != _optionsName)
        {
            return ValidateOptionsResult.Skip;
        }

        ArgumentNullException.ThrowIfNull(options);

        var validationResult = _validator.Validate(options);
        if (validationResult.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = validationResult.Errors
            .Select(error => FormatFailure(
                    typeof(TOptions).Name,
                    error.PropertyName,
                    error.ErrorMessage
                )
            );

        return ValidateOptionsResult.Fail(errors);
    }

    private static string FormatFailure(
        string optionsName,
        string propertyName,
        string errorMessage
    ) => $"Options validation failed for '{optionsName}.{propertyName}': {errorMessage}";
}
