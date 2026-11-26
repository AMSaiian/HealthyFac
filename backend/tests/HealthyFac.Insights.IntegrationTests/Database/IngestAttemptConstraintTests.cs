using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Ingest.Persistence;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.IntegrationTests.Database;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Persistence)]
public sealed class IngestAttemptConstraintTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(IngestAttemptTestData.DuplicateAttempts), MemberType = typeof(IngestAttemptTestData))]
    public async Task Save_SecondAttemptOfFile_FailsOnItsFileIndex(
        string caseName,
        Func<IngestAttempt> createAttempt,
        string expectedIndexName
    )
    {
        await _fixture.SaveAsync(createAttempt());

        var act = () => _fixture.SaveAsync(createAttempt());

        await act.Should()
            .ThrowAsync<DbUpdateException>(caseName)
            .Where(exception => exception.InnerException!.Message.Contains(expectedIndexName));
    }

    [Fact]
    public async Task Save_ActiveAttemptNextToCompletedAttemptOfFile_Succeeds()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateCompleted());

        var act = () => _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Migrate_EmbeddingIndex_IsHnswWithCosineOperators()
    {
        var definition = await _fixture.FindIndexDefinitionAsync(IngestIndexNames.ChunkEmbedding);

        definition.Should().Contain(expected: "hnsw").And.Contain(expected: "vector_cosine_ops");
    }

    [Fact]
    public async Task Delete_DocumentWithChunks_RemovesItsChunks()
    {
        var attempt = IngestAttemptTestData.CreateCompleted();
        var document = IngestAttemptTestData.CreateDocument(attempt.Id);
        await _fixture.SaveAsync(attempt, document);

        await _fixture.DeleteDocumentAsync(document.Id);

        var chunkCount = await _fixture.CountChunksAsync();
        chunkCount.Should().Be(expected: 0);
    }
}
