using Microsoft.Extensions.Options;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.UnitTests.Embeddings.Data;

namespace HealthyFac.Insights.UnitTests.Embeddings;

public sealed class ChunkEmbedderTests
{
    private const int ShortVectorSize = 768;
    private const int ManyTexts = 12;

    private readonly CountingEmbeddingGenerator _generator = new();
    private readonly ChunkEmbedder _embedder;

    public ChunkEmbedderTests()
    {
        _embedder = new ChunkEmbedder(
            _generator,
            new ChunkEmbeddingLimits
            {
                Dimensions = EmbeddingTestData.Dimensions,
                Parallelism = EmbeddingTestData.Parallelism,
            },
            new ProviderResilience(
                Options.Create(new IngestProcessingOptions { RetryDelayMilliseconds = 1 }),
                TimeProvider.System));
    }

    [Fact]
    public async Task Embed_Always_ReturnsVectorsOfLengthOneInTheOrderOfTheTexts()
    {
        var vectors = await _embedder.EmbedAsync(
            EmbeddingTestData.Texts(count: 3), TestContext.Current.CancellationToken);

        vectors.Should().HaveCount(expected: 3);
        vectors.Should().OnlyContain(vector => Math.Abs(Norm(vector) - 1f) < EmbeddingTestData.NormTolerance);
        vectors[index: 0][0].Should().BeLessThan(vectors[index: 1][0]);
        vectors[index: 1][0].Should().BeLessThan(vectors[index: 2][0]);
    }

    [Fact]
    public async Task Embed_WhenOneTextAnswers429Once_RepeatsOnlyThatText()
    {
        var texts = EmbeddingTestData.Texts(ManyTexts);
        _generator.TransientFailureText = texts[index: 3];

        var vectors = await _embedder.EmbedAsync(texts, TestContext.Current.CancellationToken);

        vectors.Should().HaveCount(ManyTexts);
        _generator.CallCount.Should().Be(ManyTexts + 1);
    }

    [Fact]
    public async Task Embed_Always_AsksForTheConfiguredDimensions()
    {
        await _embedder.EmbedAsync(EmbeddingTestData.Texts(count: 2), TestContext.Current.CancellationToken);

        _generator.Options.Should().OnlyContain(options => options!.Dimensions == EmbeddingTestData.Dimensions);
    }

    [Fact]
    public async Task Embed_WithManyTexts_RunsAtMostTheConfiguredNumberOfCallsAtOnce()
    {
        var vectors = await _embedder.EmbedAsync(
            EmbeddingTestData.Texts(ManyTexts), TestContext.Current.CancellationToken);

        vectors.Should().HaveCount(ManyTexts);
        _generator.MaxInFlight.Should().BeInRange(minimumValue: 2, EmbeddingTestData.Parallelism);
    }

    [Fact]
    public async Task Embed_WhenTheVectorHasAnotherSize_Throws()
    {
        _generator.VectorSize = ShortVectorSize;

        var act = () => _embedder.EmbedAsync(EmbeddingTestData.Texts(count: 1), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{ShortVectorSize}*");
    }

    private static float Norm(float[] vector) => (float)Math.Sqrt(vector.Sum(value => (double)value * value));
}
