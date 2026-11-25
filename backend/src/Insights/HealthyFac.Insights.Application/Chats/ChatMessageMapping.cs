using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.Application.Chats;

/// <summary>
/// Maps a stored chat and its messages to the responses of the send use cases.
/// </summary>
public static class ChatMessageMapping
{
    extension(ChatMessage message)
    {
        public ChatMessageResponse ToResponse() =>
            new()
            {
                Id = message.Id,
                Sequence = message.Sequence,
                Role = message.Role,
                Kind = message.Kind,
                State = message.State,
                Text = message.Text,
                ErrorCode = message.ErrorCode,
                Links =
                [
                    .. message.Links
                        .OrderBy(link => link.Position)
                        .Select(link => new ChatLinkResponse
                        {
                            Kind = link.Kind,
                            EntityId = link.EntityId,
                            Label = link.Label,
                            Position = link.Position,
                        }),
                ],
                Drafts =
                [
                    .. message.Drafts.Select(draft => new ChatDraftResponse
                    {
                        Kind = draft.Kind,
                        DraftId = draft.DraftId,
                        Title = draft.Title,
                        TargetMachineId = draft.TargetMachineId,
                        IsCommitted = draft.IsCommitted,
                    }),
                ],
            };
    }

    extension(Chat chat)
    {
        public ChatTurnResponse ToTurnResponse(ChatMessage user, ChatMessage assistant) =>
            new()
            {
                Chat = new ChatResponse
                {
                    Id = chat.Id,
                    Title = chat.Title,
                    State = chat.State,
                    MessageCount = chat.Messages.Count,
                },
                User = user.ToResponse(),
                Assistant = assistant.ToResponse(),
            };
    }
}
