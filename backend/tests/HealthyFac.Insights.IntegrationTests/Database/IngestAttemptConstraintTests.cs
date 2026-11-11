using HealthyFac.Insights.Infrastructure.Ingest.Persistence;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.IntegrationTests.Database;

[Collection(InsightsCollection.Name)]
public sealed class IngestAttemptConstraintTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Save_SecondActiveAttemptOfFile_FailsOnActiveFileIndex()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());

        var act = () => _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(IngestIndexNames.ActiveFile));
    }

    [Fact]
    public async Task Save_ActiveAttemptNextToCompletedAttemptOfFile_Succeeds()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateCompleted());

        var act = () => _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_SecondCompletedAttemptOfFile_FailsOnCompletedFileIndex()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateCompleted());

        var act = () => _fixture.SaveAsync(IngestAttemptTestData.CreateCompleted());

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(IngestIndexNames.CompletedFile));
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
