namespace HealthyFac.Insights.Application.Documents;

/// <summary>
/// Finds the stored document chunks of a machine that are nearest to a query vector.
/// </summary>
public interface IManualSearch
{
    /// <summary>
    /// Returns the nearest chunks by cosine distance. Only the chunks of the documents that are tied to the machine
    /// inside the company count.
    /// </summary>
    /// <param name="companyId">The company of the documents.</param>
    /// <param name="machineId">The machine that the documents are tied to.</param>
    /// <param name="query">The vector of the query, scaled to length 1.</param>
    /// <param name="count">The most chunks to return.</param>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>The chunks, nearest first.</returns>
    Task<IReadOnlyList<ManualChunk>> SearchAsync(
        Guid companyId,
        Guid machineId,
        float[] query,
        int count,
        CancellationToken cancellationToken
    );
}

public sealed record ManualChunk
{
    public Guid Id { get; init; }

    public string? Location { get; init; }

    public required string Text { get; init; }
}
