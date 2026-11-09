using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs an Azurite container for integration tests with the blob service only. A fixture holds one instance, so
/// every test project starts blob storage the same way, with the image from <see cref="TestImages"/>.
/// </summary>
/// <remarks>
/// The Testcontainers Azurite module always starts the blob, queue, and table services, so the class builds the
/// container itself and takes only the well-known account name and key from the module. The SDK default service
/// version is newer than the newest version of Azurite, so the container skips the version check.
/// </remarks>
public sealed class AzuriteTestStorage : IAsyncDisposable
{
    private const int BlobPort = 10000;
    private const string StartedMessage = "Azurite Blob service successfully listens";

    private readonly IContainer _container = new ContainerBuilder(TestImages.Azurite)
        .WithEntrypoint("azurite-blob")
        .WithCommand("--blobHost", "0.0.0.0", "--skipApiVersionCheck")
        .WithPortBinding(BlobPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(StartedMessage))
        .Build();

    public string AccountName => AzuriteBuilder.AccountName;

    public string AccountKey => AzuriteBuilder.AccountKey;

    /// <summary>
    /// The address of the storage server without the account path. Read it after <see cref="StartAsync"/>.
    /// </summary>
    public Uri Address => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(BlobPort)}/");

    /// <summary>The blob endpoint that includes the account path. Read it after <see cref="StartAsync"/>.</summary>
    public Uri BlobEndpoint =>
        new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(BlobPort)}/{AccountName}");

    /// <summary>The connection string of the running container. Read it after <see cref="StartAsync"/>.</summary>
    public string ConnectionString =>
        $"DefaultEndpointsProtocol=http;AccountName={AccountName};AccountKey={AccountKey};BlobEndpoint={BlobEndpoint};";

    public Task StartAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
