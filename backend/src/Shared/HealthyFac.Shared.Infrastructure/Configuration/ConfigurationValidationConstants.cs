namespace HealthyFac.Shared.Infrastructure.Configuration;

public static class ConfigurationValidationConstants
{
    public static string FormatFailure(
        string optionsName,
        string propertyName,
        string errorMessage
    ) => $"Options validation failed for '{optionsName}.{propertyName}': {errorMessage}";
}
