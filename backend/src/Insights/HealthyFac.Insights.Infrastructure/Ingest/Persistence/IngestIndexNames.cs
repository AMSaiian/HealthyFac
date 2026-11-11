namespace HealthyFac.Insights.Infrastructure.Ingest.Persistence;

/// <summary>
/// Defines the names of the indexes and checks of the ingest tables that code reads, so a name never differs between
/// the mapping and a check.
/// </summary>
public static class IngestIndexNames
{
    public const string ActiveFile = "ux_ingest_attempts_active_file";
    public const string CompletedFile = "ux_ingest_attempts_completed_file";
    public const string StateQueuedAt = "ix_ingest_attempts_state_queued_at";
    public const string DocumentScope = "ix_ingested_documents_company_id_machine_id";
    public const string ChunkOrdinal = "ux_document_chunks_document_id_ordinal";
    public const string ChunkEmbedding = "ix_document_chunks_embedding_hnsw";
}
