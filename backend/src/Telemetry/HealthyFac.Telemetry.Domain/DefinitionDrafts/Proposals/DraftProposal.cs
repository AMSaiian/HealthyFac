using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

/// <summary>
/// The origin that the definition drafts of one proposal share: an ingest attempt or a chat message. Each source sets
/// only its own fields. The ID is the ID of the attempt or the chat message, so a repeat of the proposal message finds
/// the same row.
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
    public Guid? ChatId { get; private set; }
    public Guid? ChatMessageId { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Creates the proposal of an ingest attempt.
    /// </summary>
    /// <param name="attemptId">The ID of the ingest attempt.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="fileId">The file that the drafts came from.</param>
    /// <returns>The proposal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftProposalInvariantErrors"/> when an ID is empty.
    /// </exception>
    public static DraftProposal ForIngest(Guid attemptId, Guid companyId, Guid fileId)
    {
        EnsureIds((nameof(attemptId), attemptId), (nameof(companyId), companyId), (nameof(fileId), fileId));

        return new DraftProposal(attemptId, companyId, DraftSource.Ingest) { AttemptId = attemptId, FileId = fileId };
    }

    /// <summary>
    /// Creates the proposal of a chat message.
    /// </summary>
    /// <param name="chatMessageId">The ID of the chat message.</param>
    /// <param name="companyId">The company of the chat.</param>
    /// <param name="chatId">The chat that the message belongs to.</param>
    /// <returns>The proposal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="DraftProposalInvariantErrors"/> when an ID is empty.
    /// </exception>
    public static DraftProposal ForChat(Guid chatMessageId, Guid companyId, Guid chatId)
    {
        EnsureIds((nameof(chatMessageId), chatMessageId), (nameof(companyId), companyId), (nameof(chatId), chatId));

        return new DraftProposal(chatMessageId, companyId, DraftSource.Chat)
        {
            ChatId = chatId,
            ChatMessageId = chatMessageId,
        };
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
