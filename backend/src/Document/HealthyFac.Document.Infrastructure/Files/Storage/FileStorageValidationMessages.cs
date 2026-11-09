namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Defines the error messages of the file storage settings.
/// </summary>
public static class FileStorageValidationMessages
{
    public static readonly string ConnectionStringKey =
        $"The connection string needs the {FileStorageConstants.ConnectionStringAccountName} and the "
        + $"{FileStorageConstants.ConnectionStringAccountKey}, because the service signs the file links with the key.";

    public static readonly string ContainerName =
        $"The container name needs {FileStorageConstants.ContainerNameMinLength} to "
        + $"{FileStorageConstants.ContainerNameMaxLength} lower case letters, digits, or single hyphens.";

    public const string PublicBlobBaseUrl = "The public blob base URL must be an absolute HTTP or HTTPS URL.";

    public static readonly string LinkLifetime =
        $"The link lifetime must be from {FileStorageConstants.LinkLifetimeMinSeconds} to "
        + $"{FileStorageConstants.LinkLifetimeMaxSeconds} seconds.";
}
