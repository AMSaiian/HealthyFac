namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Defines the names and the limits of the file storage.
/// </summary>
public static class FileStorageConstants
{
    public const string HealthCheckName = "file-storage";
    public const string ConnectionStringAccountName = "AccountName";
    public const string ConnectionStringAccountKey = "AccountKey";
    public const string DefaultContainerName = "files";
    public const int DefaultLinkLifetimeSeconds = 300;
    public const int LinkLifetimeMinSeconds = 60;
    public const int LinkLifetimeMaxSeconds = 3600;
    public const int HealthTimeoutSeconds = 2;
    public const int RetryMaxCount = 2;
    public const int RetryDelaySeconds = 1;
    public const int ContainerNameMinLength = 3;
    public const int ContainerNameMaxLength = 63;
    public const string ContainerNamePattern = "^[a-z0-9]+(-[a-z0-9]+)*$";
}
