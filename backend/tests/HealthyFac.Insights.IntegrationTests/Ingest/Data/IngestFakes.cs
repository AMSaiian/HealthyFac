using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Ingest.Processing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// The doubles of everything outside the service that the ingest worker calls: the blob, the converter, the
/// embeddings, the machine signals, and the chat model. A test changes the behavior of one double, and the fixture
/// resets all of them before the next test.
/// </summary>
public sealed class IngestFakes
{
    public FakeBlobStreamSource Blob { get; } = new();

    public FakeDocumentConverter Converter { get; } = new();

    public FakeEmbeddingGenerator Embeddings { get; } = new();

    public FakeMachineSignalCatalog Signals { get; } = new();

    public ScriptedChatClient Chat { get; } = new();

    public TestIngestOptions Options { get; } = new();

    public void Reset()
    {
        Blob.Reset();
        Converter.Reset();
        Embeddings.Reset();
        Signals.Reset();
        Chat.Reset();
        Options.Reset();
    }

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IBlobStreamSource>(Blob);
        services.AddSingleton<IDocumentConverter>(Converter);
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(Embeddings);
        services.AddSingleton<IMachineSignalCatalog>(Signals);
        services.AddSingleton<IChatClient>(Chat);
        services.AddSingleton<IOptions<IngestProcessingOptions>>(Options);
    }
}
