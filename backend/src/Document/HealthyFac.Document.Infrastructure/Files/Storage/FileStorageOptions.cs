using FluentValidation;

namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Configures the blob storage of the files.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>The storage connection string. It holds the account name and the account key.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    public string ContainerName { get; init; } = FileStorageConstants.DefaultContainerName;

    /// <summary>
    /// The public URL that the gateway serves for the blobs. A file link is this URL, the container, the blob name,
    /// and the signature.
    /// </summary>
    public string PublicBlobBaseUrl { get; init; } = string.Empty;

    /// <summary>The time that a file link stays valid.</summary>
    public int LinkLifetimeSeconds { get; init; } = FileStorageConstants.DefaultLinkLifetimeSeconds;
}

public sealed class FileStorageOptionsValidator : AbstractValidator<FileStorageOptions>
{
    public FileStorageOptionsValidator()
    {
        RuleFor(options => options.ConnectionString)
            .Must(HasAccountNameAndKey)
            .WithMessage(FileStorageValidationMessages.ConnectionStringKey);

        RuleFor(options => options.ContainerName)
            .Length(FileStorageConstants.ContainerNameMinLength, FileStorageConstants.ContainerNameMaxLength)
            .Matches(FileStorageConstants.ContainerNamePattern)
            .WithMessage(FileStorageValidationMessages.ContainerName);

        RuleFor(options => options.PublicBlobBaseUrl)
            .Must(IsAbsoluteWebUrl)
            .WithMessage(FileStorageValidationMessages.PublicBlobBaseUrl);

        RuleFor(options => options.LinkLifetimeSeconds)
            .InclusiveBetween(FileStorageConstants.LinkLifetimeMinSeconds, FileStorageConstants.LinkLifetimeMaxSeconds)
            .WithMessage(FileStorageValidationMessages.LinkLifetime);
    }

    private static bool HasAccountNameAndKey(string connectionString)
    {
        var settings = FileStorageConnectionString.Parse(connectionString);

        return settings.ContainsKey(FileStorageConstants.ConnectionStringAccountName)
            && settings.ContainsKey(FileStorageConstants.ConnectionStringAccountKey);
    }

    private static bool IsAbsoluteWebUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
