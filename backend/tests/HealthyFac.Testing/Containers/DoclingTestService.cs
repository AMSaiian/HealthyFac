using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a docling-serve container for integration tests, with the image from <see cref="TestImages"/>. Without a
/// GPU the image runs on the CPU. The container needs a long start time, because the service loads its models.
/// </summary>
public sealed class DoclingTestService : IAsyncDisposable
{
    private const ushort ContainerPort = 5001;
    private const string HealthPath = "/health";
    private const string ApiKeyValue = "healthyfac-test-docling-key";

    private readonly IContainer _container = new ContainerBuilder(TestImages.Docling)
        .WithPortBinding(ContainerPort, assignRandomHostPort: true)
        .WithEnvironment(name: "DOCLING_SERVE_API_KEY", ApiKeyValue)
        .WithEnvironment(name: "DOCLING_SERVE_ENG_LOC_NUM_WORKERS", value: "1")
        .WithEnvironment(name: "DOCLING_SERVE_ENABLE_UI", value: "false")
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(ContainerPort)
                    .ForPath(HealthPath)))
        .Build();

    /// <summary>The base address of the running container. Read it after <see cref="StartAsync"/>.</summary>
    public Uri BaseUri => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(ContainerPort)}");

    /// <summary>The value for the <c>X-Api-Key</c> header.</summary>
    public string ApiKey => ApiKeyValue;

    public Task StartAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
