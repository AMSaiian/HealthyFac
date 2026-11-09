using Azure.Core;
using Azure.Storage.Blobs;
using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Registers the file storage.
/// </summary>
public static class FileStorageExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated storage settings, one shared blob service client, and the blob storage.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddFileStorage(IConfiguration configuration)
        {
            services.AddValidatedOptions<FileStorageOptions, FileStorageOptionsValidator>(
                FileStorageOptions.SectionName, configuration);

            services.AddSingleton(provider =>
                new BlobServiceClient(
                    provider.GetRequiredService<IOptions<FileStorageOptions>>().Value.ConnectionString,
                    new BlobClientOptions
                    {
                        Retry =
                        {
                            Mode = RetryMode.Exponential,
                            MaxRetries = FileStorageConstants.RetryMaxCount,
                            Delay = TimeSpan.FromSeconds(FileStorageConstants.RetryDelaySeconds),
                        },
                    }));
            services.AddSingleton<BlobFileStorage>();
            services.AddSingleton<IFileStorage>(provider => provider.GetRequiredService<BlobFileStorage>());

            return services;
        }
    }
}
