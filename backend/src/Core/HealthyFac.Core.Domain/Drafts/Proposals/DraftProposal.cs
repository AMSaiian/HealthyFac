using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Drafts.Proposals;

/// <summary>
/// The origin that one or more drafts share: an ingest attempt, a run of a case, or a chat message. Each source sets
/// only its own fields. The ID is the ID of the attempt, the run, or the chat message, so a repeat of the proposal
/// message finds the same row.
/// </summary>
public class DraftProposal : IEntity, IAuditable, ISoftDeletable
{
    private DraftProposal(Guid id, Guid companyId, DraftSource source)
    {
        Id = id;
        CompanyId = companyId;
        Source = source;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public DraftSource Source { get; private set; }
    public Guid? AttemptId { get; private set; }
    public Guid? FileId { get; private set; }

    /// <summary>The time when the user queued the ingest (UTC).</summary>
    public DateTime? QueuedAt { get; private set; }

    public Guid? CaseId { get; private set; }
    public Guid? RunId { get; private set; }
    public Guid? ChatId { get; private set; }
    public Guid? ChatMessageId { get; private set; }

    /// <summary>The display name of the chat owner. Set for the source chat only.</summary>
    public string? RequestedByName { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Creates the proposal of an ingest attempt.
    /// </summary>
    /// <param name="id">The ID of the ingest attempt.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="fileId">The file that the drafts came from.</param>
    /// <param name="queuedAt">The time when the user queued the ingest (UTC).</param>
    /// <returns>The proposal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftProposalInvariantErrors"/> when an ID is empty.
    /// </exception>
    public static DraftProposal ForIngest(Guid id, Guid companyId, Guid fileId, DateTime queuedAt)
    {
        EnsureIds((nameof(id), id), (nameof(companyId), companyId), (nameof(fileId), fileId));

        return new DraftProposal(id, companyId, DraftSource.Ingest)
        {
            AttemptId = id,
            FileId = fileId,
            QueuedAt = queuedAt,
        };
    }

    /// <summary>
    /// Creates the proposal of a run of a case.
    /// </summary>
    /// <param name="id">The ID of the run.</param>
    /// <param name="companyId">The company of the case.</param>
    /// <param name="caseId">The case that the run belongs to.</param>
    /// <returns>The proposal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftProposalInvariantErrors"/> when an ID is empty.
    /// </exception>
    public static DraftProposal ForCase(Guid id, Guid companyId, Guid caseId)
    {
        EnsureIds((nameof(id), id), (nameof(companyId), companyId), (nameof(caseId), caseId));

        return new DraftProposal(id, companyId, DraftSource.Case) { CaseId = caseId, RunId = id };
    }

    /// <summary>
    /// Creates the proposal of a chat message.
    /// </summary>
    /// <param name="id">The ID of the chat message.</param>
    /// <param name="companyId">The company of the chat.</param>
    /// <param name="chatId">The chat that the message belongs to.</param>
    /// <param name="requestedByName">The display name of the chat owner.</param>
    /// <returns>The proposal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftProposalInvariantErrors"/> when an ID is empty.
    /// </exception>
    public static DraftProposal ForChat(Guid id, Guid companyId, Guid chatId, string? requestedByName = null)
    {
        EnsureIds((nameof(id), id), (nameof(companyId), companyId), (nameof(chatId), chatId));

        return new DraftProposal(id, companyId, DraftSource.Chat)
            { ChatId = chatId, ChatMessageId = id, RequestedByName = requestedByName };
    }

    private static void EnsureIds(params (string Name, Guid Value)[] ids)
    {
        foreach (var (name, value) in ids)
        {
            if (value == Guid.Empty)
            {
                throw DraftProposalInvariantErrors.EmptyId(name);
            }
        }
    }
}
