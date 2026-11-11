using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Documents;

/// <summary>
/// The stored result of a completed ingest of one file: the parent of the chunks. The ID is the ID of the file, so one
/// file has at most one document. Search joins the chunks to the document to filter by company and machine.
/// </summary>
public class IngestedDocument : IEntity
{
    private readonly List<DocumentChunk> _chunks = [];

    /// <summary>
    /// Creates the document. Add the chunks with <see cref="AddChunk"/>.
    /// </summary>
    /// <param name="id">The ID of the file. It is the ID of the document.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="machineId">The machine of the file, or <see langword="null"/>.</param>
    /// <param name="machineChangedAt">The time of the last tie change of the file (UTC).</param>
    /// <param name="attemptId">The attempt that produced the document.</param>
    /// <param name="extension">The lower case extension of the file.</param>
    /// <param name="pageCount">The number of pages.</param>
    public IngestedDocument(
        Guid id,
        Guid companyId,
        Guid? machineId,
        DateTime machineChangedAt,
        Guid attemptId,
        string extension,
        int pageCount
    )
    {
        Id = id;
        CompanyId = companyId;
        MachineId = machineId;
        MachineChangedAt = machineChangedAt;
        AttemptId = attemptId;
        Extension = extension;
        PageCount = pageCount;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? MachineId { get; private set; }
    public DateTime MachineChangedAt { get; private set; }
    public Guid AttemptId { get; private set; }
    public string Extension { get; private set; }
    public int PageCount { get; private set; }
    public int ChunkCount { get; private set; }

    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks;

    /// <summary>
    /// Adds the next chunk.
    /// </summary>
    /// <param name="location">The page, sheet, or slide and the heading path, or <see langword="null"/>.</param>
    /// <param name="text">The text of the chunk.</param>
    /// <param name="embedding">The unit length embedding of the text.</param>
    public void AddChunk(string? location, string text, float[] embedding)
    {
        _chunks.Add(new DocumentChunk(Id, _chunks.Count, location, text, embedding));
        ChunkCount = _chunks.Count;
    }

    /// <summary>
    /// Stores the machine of the file. A change that is not newer than the stored one changes nothing.
    /// </summary>
    /// <param name="machineId">The machine, or <see langword="null"/> when the file is untied.</param>
    /// <param name="changedAt">The time of the change in Document (UTC).</param>
    public void ChangeMachine(Guid? machineId, DateTime changedAt)
    {
        if (changedAt <= MachineChangedAt)
        {
            return;
        }

        MachineId = machineId;
        MachineChangedAt = changedAt;
    }
}
