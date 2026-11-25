using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.Messaging;

namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// Stores the draft links of a reply and sends the proposals of the turn. Only a completed turn sends them: its rows
/// are committed, and the messages go through the outbox with the same save as the reply. Any other ending keeps the
/// rows uncommitted and sends nothing, so a cancelled or failed turn creates no draft in Core or Telemetry.
/// </summary>
/// <param name="buffer">Holds the proposals of the turn.</param>
/// <param name="outbox">Sends the proposals with the final save.</param>
public sealed class ChatDraftCommit(ChatDraftBuffer buffer, IOutbox outbox)
{
    private readonly ChatDraftBuffer _buffer = buffer;
    private readonly IOutbox _outbox = outbox;

    /// <summary>
    /// Adds the draft rows to the reply and the messages to the outbox. The caller saves.
    /// </summary>
    /// <param name="chat">The chat of the turn.</param>
    /// <param name="assistantMessage">The reply that holds the draft rows.</param>
    /// <param name="isCompleted">Tells whether the turn ended completed.</param>
    public void Stage(Chat chat, ChatMessage assistantMessage, bool isCompleted)
    {
        if (_buffer.IsEmpty)
        {
            return;
        }

        foreach (var row in _buffer.DraftRows)
        {
            if (isCompleted)
            {
                row.Commit();
            }

            assistantMessage.AddDraft(row);
        }

        if (!isCompleted)
        {
            return;
        }

        if (_buffer.Machines.Count + _buffer.Metrics.Count + _buffer.Statuses.Count + _buffer.Phases.Count > 0)
        {
            _outbox.Add(
                new DraftsProposed
                {
                    Source = ProposalSource.Chat,
                    CompanyId = chat.CompanyId,
                    ChatId = chat.Id,
                    ChatMessageId = assistantMessage.Id,
                    Machines = _buffer.Machines,
                    Metrics = _buffer.Metrics,
                    Statuses = _buffer.Statuses,
                    Phases = _buffer.Phases,
                });
        }

        foreach (var workOrder in _buffer.WorkOrders)
        {
            _outbox.Add(
                new WorkOrderDraftProposed
                {
                    DraftId = workOrder.DraftId,
                    CompanyId = chat.CompanyId,
                    Source = WorkOrderProposalSource.Chat,
                    MachineId = workOrder.MachineId,
                    ChatId = chat.Id,
                    ChatMessageId = assistantMessage.Id,
                    Title = workOrder.Title,
                    Description = workOrder.Description,
                    Priority = workOrder.Priority,
                    Category = workOrder.Category,
                    EstimateMinutes = workOrder.EstimateMinutes,
                });
        }
    }
}
