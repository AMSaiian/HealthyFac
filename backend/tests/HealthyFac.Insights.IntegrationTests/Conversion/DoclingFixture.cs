using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.Infrastructure.Conversion;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Resetting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.IntegrationTests.Conversion;

/// <summary>
/// The collection fixture of the real Docling test. It starts one docling-serve container and builds the real
/// converter against it. The container keeps no state between tests, so the reset does nothing.
/// </summary>
public sealed class DoclingFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly DoclingTestService _docling = new();

    private ServiceProvider _services = null!;

    public IDocumentConverter Converter => _services.GetRequiredService<IDocumentConverter>();

    public async ValueTask InitializeAsync()
    {
        await _docling.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{DoclingOptions.SectionName}:BaseUrl"] = _docling.BaseUri.ToString(),
                    [$"{DoclingOptions.SectionName}:ApiKey"] = _docling.ApiKey,
                })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProviderCalls>(
            new ProviderResilience(Options.Create(new IngestProcessingOptions()), TimeProvider.System));
        services.AddDoclingConverter(configuration);
        _services = services.BuildServiceProvider();
    }

    public ValueTask ResetAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _docling.DisposeAsync();
    }
}
