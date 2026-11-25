namespace HealthyFac.Insights.Application.Documents;

/// <summary>
/// Finds the manual chunks of a machine for a query vector. A chunk that only holds titles
/// (<see cref="ManualChunkText.IsTitleOnly"/>) never comes back, so the search asks for more chunks until the count is
/// full or the chunks end. The planner tool and the chat tool share it.
/// </summary>
public static class ManualChunkFinder
{
    /// <summary>
    /// Finds the nearest chunks that hold more than titles.
    /// </summary>
    /// <param name="search">Finds the nearest chunks.</param>
    /// <param name="companyId">The company of the documents.</param>
    /// <param name="machineId">The machine that the documents are tied to.</param>
    /// <param name="vector">The vector of the query.</param>
    /// <param name="wanted">The number of chunks to return.</param>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>At most <paramref name="wanted"/> chunks, nearest first.</returns>
    public static async Task<IReadOnlyList<ManualChunk>> FindAsync(
        IManualSearch search,
        Guid companyId,
        Guid machineId,
        float[] vector,
        int wanted,
        CancellationToken cancellationToken
    )
    {
        var asked = wanted;

        while (true)
        {
            var found = await search.SearchAsync(companyId, machineId, vector, asked, cancellationToken);
            var kept = found.Where(chunk => !ManualChunkText.IsTitleOnly(chunk.Text)).Take(wanted).ToList();

            if (kept.Count == wanted || found.Count < asked)
            {
                return kept;
            }

            asked += wanted;
        }
    }
}
