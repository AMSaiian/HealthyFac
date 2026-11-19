using HealthyFac.Insights.Application.Documents;
using HealthyFac.Insights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace HealthyFac.Insights.Infrastructure.Documents;

/// <summary>
/// Finds the nearest chunks with the cosine distance operator of pgvector, which the HNSW index of the chunk table
/// serves. Only the chunks of the documents of the company that are tied to the machine count.
/// </summary>
/// <param name="dbContext">Runs the query.</param>
public sealed class PgvectorManualSearch(InsightsDbContext dbContext) : IManualSearch
{
    private readonly InsightsDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<ManualChunk>> SearchAsync(
        Guid companyId,
        Guid machineId,
        float[] query,
        int count,
        CancellationToken cancellationToken
    )
    {
        var vector = new Vector(query);

        return await _dbContext.Database
            .SqlQuery<ManualChunk>(
                $"""
                 SELECT c.id AS "Id", c.location AS "Location", c.text AS "Text"
                 FROM document_chunks c
                 JOIN ingested_documents d ON d.id = c.document_id
                 WHERE d.company_id = {companyId} AND d.machine_id = {machineId}
                 ORDER BY c.embedding <=> {vector}
                 LIMIT {count}
                 """)
            .ToListAsync(cancellationToken);
    }
}
