using Google.GenAI.Types;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.UnitTests.Embeddings.Data;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.UnitTests.Embeddings;

public sealed class RetrievalDocumentEmbeddingGeneratorTests
{
    [Fact]
    public async Task Generate_Always_SetsTheTaskTypeAndTheDimensionsOnTheRawConfig()
    {
        var inner = new CountingEmbeddingGenerator { Delay = TimeSpan.Zero };
        var generator = new RetrievalDocumentEmbeddingGenerator(inner);

        await generator.GenerateAsync(
            ["0"],
            new EmbeddingGenerationOptions { Dimensions = EmbeddingTestData.Dimensions },
            TestContext.Current.CancellationToken);

        var sent = inner.Options.Should().ContainSingle().Which!;
        var config = sent.RawRepresentationFactory!(inner).Should().BeOfType<EmbedContentConfig>().Which;
        config.TaskType.Should().Be(RetrievalDocumentEmbeddingGenerator.TaskType);
        config.OutputDimensionality.Should().Be(EmbeddingTestData.Dimensions);
    }

    [Fact]
    public async Task Generate_Always_LeavesTheOptionsOfTheCallerUnchanged()
    {
        var inner = new CountingEmbeddingGenerator { Delay = TimeSpan.Zero };
        var generator = new RetrievalDocumentEmbeddingGenerator(inner);
        var options = new EmbeddingGenerationOptions { Dimensions = EmbeddingTestData.Dimensions };

        await generator.GenerateAsync(["0"], options, TestContext.Current.CancellationToken);

        options.RawRepresentationFactory.Should().BeNull();
    }
}
